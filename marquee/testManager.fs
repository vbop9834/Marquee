module testManager

type TestDescription = string
type TestFunction = marquee.Browser -> unit
type Test = TestDescription*TestFunction

type TestResult =
| TestFailed of TestDescription*System.Exception
| TestPassed

type ReportFunction = TestResult -> unit

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
                  TestFailed(testDescription, ex)
              browser.instance.Close()
              testResult |> reportFunction 
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

type TestSupervisorMsgs =
| Report of TestWorkerId*TestResult*AsyncReplyChannel<unit>
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
      let reportFunction : ReportFunction = fun testResult ->
        (fun r -> Report(testWorkerId, testResult, r)) |> inbox.PostAndReply
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

  static member Create browserType amountOfBrowsers testQueue =
    let testWorkers : TestWorkers =
      [1..amountOfBrowsers] 
      |> List.map(fun workerId -> (workerId, TestWorker.Create browserType))
      |> Map.ofList
    let testSupervisorInstance = TestSupervisorInstance.Start(fun inbox ->
      let rec loop (workers : TestWorkers) testQueue (maybeEndOfWorkReplyChannel : AsyncReplyChannel<unit> option) =
        async {
          let! msg = inbox.Receive ()
          match msg with
          | Report (testWorkerId, testResult, replyChannel) ->
            replyChannel.Reply()
            let testQueue, workers = TestSupervisor.SendTestToWorker inbox maybeEndOfWorkReplyChannel testQueue testWorkerId workers 
             
            //TODO make a reporter instance
            match testResult with
            | TestPassed -> ()
            | TestFailed (testDescription, testException) ->
              printfn "Failure - %s" testDescription
              printfn "Exception %s" testException.StackTrace
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

type TestManagerMsgs =
| Register of Test*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| EndManager

type TestManagerInstance = MailboxProcessor<TestManagerMsgs>

type TestManager =
  { 
    mailbox : TestManagerInstance
  }

  static member Create amountOfBrowsers (browserType : marquee.BrowserType) =
    let testManager = 
      TestManagerInstance.Start(fun inbox ->
        let rec loop registeredTests =
          async {
            let! msg = inbox.Receive()
            match msg with
            | Register (test, replyChannel) ->
              let registeredTests = registeredTests |> List.append [test] 
              replyChannel.Reply ()
              return! loop registeredTests
            | RunTests replyChannel ->
              let testSupervisor = TestSupervisor.Create browserType amountOfBrowsers registeredTests 
              testSupervisor.RunTests ()
              replyChannel.Reply ()
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

  member this.EndManager () =
    EndManager |> this.mailbox.Post
