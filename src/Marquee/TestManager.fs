module TestManager

type TestDescription = string
type TestFunction = Marquee.Browser -> unit
type Test = TestDescription*TestFunction

type TestResult =
| TestFailed of System.Exception
| TestPassed
type TestResultPackage = TestDescription*TestResult
type TestResultPackages = TestResultPackage list
type ExecutionTime = System.TimeSpan

type ReportFunction = TestResultPackage -> unit
type TestResultsFunction = ExecutionTime -> TestResultPackages -> int
type InfoFunction = string -> unit

type AmountOfBrowsers =
| MaximumBrowsersPossible
| IWantThisManyBrowsers of int

//Test Worker
//=========================================================================
type private TestWorkerId = int

type private TestWorkerMsgs =
| Work of Test*ReportFunction*AsyncReplyChannel<unit>
| EndWorker

type private TestWorkerInstance = MailboxProcessor<TestWorkerMsgs>

type private TestWorker =
  { TestWorkerInstance : TestWorkerInstance }

  static member Create (browserConfiguration : Marquee.BrowserConfiguration) =
    let testWorkerInstance =
      TestWorkerInstance.Start(fun inbox ->
        let rec loop () =
          async {
            let! msg = inbox.Receive()
            match msg with
            | Work ((testDescription, testFunction), reportFunction, replyChannel) ->
              replyChannel.Reply()
              let testResult : TestResult =
                try
                  let browser = browserConfiguration |> Marquee.Browser.Create
                  browser |> testFunction
                  browser.Quit()
                  TestPassed
                with
                | ex ->
                  TestFailed(ex)
              (testDescription,testResult) |> reportFunction
              return! loop ()
            | EndWorker ->
              ()
          }
        loop ()
      )
    { TestWorkerInstance = testWorkerInstance }

    member this.Work test reportFunction =
      (fun r -> Work(test, reportFunction, r)) |> this.TestWorkerInstance.PostAndReply

    member this.Die () =
      EndWorker |> this.TestWorkerInstance.Post
//=========================================================================

//Test Reporter
//=========================================================================
type private TestReporterMsgs =
| GatherTestResultPackage of TestResultPackage*AsyncReplyChannel<unit>
| SendResultsToResultsFunction of AsyncReplyChannel<int>
| EndReporter

type private TestReporterInstance = MailboxProcessor<TestReporterMsgs>

type private TestReporter =
  { TestReporterInstance : TestReporterInstance }

  static member Create resultsFunction =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let reporterInstance =
      TestReporterInstance.Start(fun inbox ->
        let rec loop (resultPackages : TestResultPackages) =
          async {
            let! msg = inbox.Receive ()
            match msg with
            | GatherTestResultPackage (resultPackage, replyChannel) ->
              let resultPackages = List.append resultPackages [resultPackage]
              replyChannel.Reply ()
              return! loop resultPackages
            | SendResultsToResultsFunction replyChannel ->
              let testRunTime = stopwatch.Elapsed
              let exitCode = resultPackages |> resultsFunction testRunTime
              replyChannel.Reply exitCode
              return! loop List.empty
            | EndReporter -> ()
          }
        loop List.empty
      )
    { TestReporterInstance = reporterInstance }

    member this.GatherTestResultPackage resultPackage =
      (fun r -> GatherTestResultPackage(resultPackage, r)) |> this.TestReporterInstance.PostAndReply

    member this.SendResultsToResultsFunction () =
      SendResultsToResultsFunction |> this.TestReporterInstance.PostAndReply
//=========================================================================

//Test Supervisor
//=========================================================================
type private TestSupervisorMsgs =
| Report of TestWorkerId*TestResultPackage*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| EndSupervisor

type private TestSupervisorInstance = MailboxProcessor<TestSupervisorMsgs>

type private TestWorkers = Map<TestWorkerId,TestWorker>

type private TestSupervisor =
  { TestSupervisorInstance : TestSupervisorInstance }

  static member Create (testReporter : TestReporter) (browserConfiguration : Marquee.BrowserConfiguration) (amountOfBrowsers : AmountOfBrowsers) testQueue =
    let testWorkers : TestWorkers =
      let amountOfBrowsers = 
        match amountOfBrowsers with
        | MaximumBrowsersPossible -> List.length testQueue
        | IWantThisManyBrowsers amountOfBrowsers -> amountOfBrowsers
      [1..amountOfBrowsers] 
      |> List.map(fun workerId -> (workerId, TestWorker.Create browserConfiguration))
      |> Map.ofList
    let testSupervisorInstance = TestSupervisorInstance.Start(fun inbox ->
      let rec loop (workers : TestWorkers) testQueue (maybeEndOfWorkReplyChannel : AsyncReplyChannel<unit> option) =
        async {
          let! msg = inbox.Receive ()
          match msg with
          | Report (testWorkerId, testResultPackage, replyChannel) ->
            replyChannel.Reply()
            //Report results
            testReporter.GatherTestResultPackage testResultPackage
            //Send work to idle worker
            let testQueue, workers = TestSupervisor.SendTestToWorker inbox maybeEndOfWorkReplyChannel testQueue testWorkerId workers 
            //Recurse with new testQueue and worker pool
            return! loop workers testQueue maybeEndOfWorkReplyChannel
          | RunTests replyChannel ->
            //Create option replyChannel for closing when all work is done
            let maybeEndOfWorkReplyChannel = Some replyChannel
            //Send test to workers
            let testQueue, workers = TestSupervisor.SendWorkToWorkers inbox maybeEndOfWorkReplyChannel testQueue workers 
            return! loop workers testQueue maybeEndOfWorkReplyChannel
          | EndSupervisor ->
            ()
        }
      loop testWorkers testQueue None
    )
    { TestSupervisorInstance = testSupervisorInstance }


  static member SendTestToWorker (inbox : TestSupervisorInstance) (maybeEndOfWorkReplyChannel : AsyncReplyChannel<unit> option) testQueue testWorkerId (workers : TestWorkers) =
    let worker = workers |> Map.find testWorkerId
    match testQueue with
    | test :: testQueue ->
      let reportFunction : ReportFunction = fun testResultPackage ->
        (fun r -> Report(testWorkerId, testResultPackage, r)) |> inbox.PostAndReply
      worker.Work test reportFunction
      (testQueue, workers)
    | [] ->
      //Kill worker
      worker.Die ()
      //Remove worker from map
      let workers = workers |> Map.remove testWorkerId
      //Check if all workers have gone home
      match workers |> Map.isEmpty with
      | true ->
        //reply on parent channel
        match maybeEndOfWorkReplyChannel with
        | Some replyChannel -> replyChannel.Reply ()
        | None -> ()
        //End Supervisor
        EndSupervisor |> inbox.Post
      | false ->
        //Continue existing. workers are working
        ()
      (List.empty, workers)

  static member SendWorkToWorkers (inbox : TestSupervisorInstance) (maybeEndOfWorkReplyChannel : AsyncReplyChannel<unit> option) testQueue (workers : TestWorkers) =
    let rec sendWork workersLeft testQueue workers =
      match workersLeft with
      | [] -> testQueue, workers
      | testWorkerId :: workersLeft ->
        let testQueue, workers = TestSupervisor.SendTestToWorker inbox maybeEndOfWorkReplyChannel testQueue testWorkerId workers 
        sendWork workersLeft testQueue workers
    let workerIds = workers |> Map.toList |> List.map fst
    sendWork workerIds testQueue workers

  member this.RunTests () =
    RunTests |> this.TestSupervisorInstance.PostAndReply
//=========================================================================

//Test Manager
//=========================================================================
type private TestManagerMsgs =
| Register of Test*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| ReportResults of AsyncReplyChannel<int>
| EndManager

type private TestManagerInstance = MailboxProcessor<TestManagerMsgs>

type TestManagerConfiguration =
  {
    InfoFunction : InfoFunction
    TestResultsFunction : TestResultsFunction
    AmountOfBrowsers : AmountOfBrowsers
    BrowserType : Marquee.BrowserType
    ElementTimeout : int
    AssertionTimeout : int
  }

type TestManager =
  { 
    Mailbox : TestManagerInstance
  }

  static member Create (configuration : TestManagerConfiguration) =
    let testReporter = TestReporter.Create configuration.TestResultsFunction
    let testManager = 
      TestManagerInstance.Start(fun inbox ->
        let rec loop registeredTests =
          async {
            let! msg = inbox.Receive()
            match msg with
            | Register (test, replyChannel) ->
              let registeredTests = List.append registeredTests [test] 
              replyChannel.Reply ()
              return! loop registeredTests
            | RunTests replyChannel ->
              "Starting Tests" |> configuration.InfoFunction
              let browserConfiguration : Marquee.BrowserConfiguration =
                {
                  BrowserType = configuration.BrowserType
                  ElementTimeout = configuration.ElementTimeout
                  AssertionTimeout = configuration.AssertionTimeout
                }
              let testSupervisor = TestSupervisor.Create testReporter browserConfiguration configuration.AmountOfBrowsers registeredTests 
              testSupervisor.RunTests ()
              replyChannel.Reply ()
              return! loop registeredTests
            | ReportResults replyChannel ->
              "Reporting Results" |> configuration.InfoFunction
              let exitCode = testReporter.SendResultsToResultsFunction ()
              sprintf "Exiting with exit code %i" exitCode |> configuration.InfoFunction
              replyChannel.Reply exitCode
              return! loop registeredTests
            | EndManager -> ()
          }
        loop List.empty
      )
    { Mailbox = testManager }

  member this.Register testDescription testFunction =
    let test = testDescription, testFunction
    (fun r -> Register(test, r)) |> this.Mailbox.PostAndReply

  member this.RunTests () =
   RunTests |> this.Mailbox.PostAndReply

  member this.ReportResults () =
    ReportResults |> this.Mailbox.PostAndReply

  member this.EndManager () =
    EndManager |> this.Mailbox.Post
//=========================================================================

