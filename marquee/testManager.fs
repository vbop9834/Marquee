module testManager

type TestDescription = string
type TestFunction = marquee.Browser -> unit
type Test = TestDescription*TestFunction

type TestResult =
| TestFailed of System.Exception
| TestPassed
type TestResultPackage = TestDescription*TestResult
type TestResultPackages = TestResultPackage list
type ExecutionTime = System.TimeSpan

type ReportFunction = TestResultPackage -> unit
type TestResultsFunction = ExecutionTime -> TestResultPackages -> int

//Test Worker
//=========================================================================
type TestWorkerId = int

type TestWorkerMsgs =
| Work of Test*ReportFunction*AsyncReplyChannel<unit>
| EndWorker

type TestWorkerInstance = MailboxProcessor<TestWorkerMsgs>

type TestWorker =
  { testWorkerInstance : TestWorkerInstance }

  static member Create browserType =
    let testWorkerInstance =
      TestWorkerInstance.Start(fun inbox ->
        let rec loop () =
          async {
            let! msg = inbox.Receive()
            match msg with
            | Work ((testDescription, testFunction), reportFunction, replyChannel) ->
              replyChannel.Reply()
              let browser = browserType |> marquee.Browser.Create
              let testResult : TestResult =
                try
                  browser |> testFunction
                  TestPassed
                with
                | ex ->
                  TestFailed(ex)
              browser.instance.Close()
              (testDescription,testResult) |> reportFunction 
              return! loop ()
            | EndWorker ->
              ()
          }
        loop ()
      )
    { testWorkerInstance = testWorkerInstance }

    member this.Work test reportFunction =
      (fun r -> Work(test, reportFunction, r)) |> this.testWorkerInstance.PostAndReply

    member this.Die () =
      EndWorker |> this.testWorkerInstance.Post
//=========================================================================

//Test Reporter
//=========================================================================
type TestReporterMsgs =
| GatherTestResultPackage of TestResultPackage*AsyncReplyChannel<unit>
| SendResultsToResultsFunction of AsyncReplyChannel<int>
| EndReporter

type TestReporterInstance = MailboxProcessor<TestReporterMsgs>

type TestReporter =
  { testReporterInstance : TestReporterInstance }

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
    { testReporterInstance = reporterInstance }

    member this.GatherTestResultPackage resultPackage =
      (fun r -> GatherTestResultPackage(resultPackage, r)) |> this.testReporterInstance.PostAndReply

    member this.SendResultsToResultsFunction () =
      SendResultsToResultsFunction |> this.testReporterInstance.PostAndReply
//=========================================================================

//Test Supervisor
//=========================================================================
type TestSupervisorMsgs =
| Report of TestWorkerId*TestResultPackage*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| EndSupervisor

type TestSupervisorInstance = MailboxProcessor<TestSupervisorMsgs>

type private TestWorkers = Map<TestWorkerId,TestWorker>

type TestSupervisor =
  { testSupervisorInstance : TestSupervisorInstance }

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

  static member Create (testReporter : TestReporter) browserType amountOfBrowsers testQueue =
    let testWorkers : TestWorkers =
      [1..amountOfBrowsers] 
      |> List.map(fun workerId -> (workerId, TestWorker.Create browserType))
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
    { testSupervisorInstance = testSupervisorInstance }

  member this.RunTests () =
    RunTests |> this.testSupervisorInstance.PostAndReply
//=========================================================================

//Test Manager
//=========================================================================
type TestManagerMsgs =
| Register of Test*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| ReportResults of AsyncReplyChannel<int>
| EndManager

type TestManagerInstance = MailboxProcessor<TestManagerMsgs>

type TestManager =
  { 
    mailbox : TestManagerInstance
  }

  static member Create (resultsFunction : TestResultsFunction) amountOfBrowsers (browserType : marquee.BrowserType) =
    let testReporter = TestReporter.Create resultsFunction
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
              let testSupervisor = TestSupervisor.Create testReporter browserType amountOfBrowsers registeredTests 
              testSupervisor.RunTests ()
              replyChannel.Reply ()
              return! loop registeredTests
            | ReportResults replyChannel ->
              let exitCode = testReporter.SendResultsToResultsFunction ()
              replyChannel.Reply exitCode
              return! loop registeredTests
            | EndManager -> ()
          }
        loop List.empty
      )
    { mailbox = testManager }

  member this.Register test =
    (fun r -> Register(test, r)) |> this.mailbox.PostAndReply

  member this.RunTests () =
   RunTests |> this.mailbox.PostAndReply

  member this.ReportResults () =
    ReportResults |> this.mailbox.PostAndReply

  member this.EndManager () =
    EndManager |> this.mailbox.Post
//=========================================================================
