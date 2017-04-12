module testManager

type TestDescription = string
type TestFunction = marquee.Browser -> unit
type Test = TestDescription*TestFunction

type TestManagerMsgs =
| Register of Test*AsyncReplyChannel<unit>
| RunTests of AsyncReplyChannel<unit>
| EndManager

type TestManagerInstance = MailboxProcessor<TestManagerMsgs>

type TestManager =
  { 
    mailbox : TestManagerInstance
  }

  static member private ExecuteTests browserType tests =
    let runTest (testDescription, testFunc) =
      let browser = browserType |> marquee.Browser.Create
      try 
        testFunc browser
      with
      | ex ->
       //TODO this is where output registration goes for now print to console
       printfn "Failure - %s" testDescription
       printfn "Exception %s" ex.StackTrace
      browser.instance.Close ()
    tests
    |> Array.Parallel.iter runTest

  static member Create (browserType : marquee.BrowserType) =
    let testManager = 
      TestManagerInstance.Start(fun inbox ->
        let rec loop registeredTests =
          async {
            let! msg = inbox.Receive()
            match msg with
            | Register (test, replyChannel) ->
              let registeredTests = registeredTests |> Array.append [|test|] 
              replyChannel.Reply ()
              return! loop registeredTests
            | RunTests replyChannel ->
              registeredTests |> TestManager.ExecuteTests browserType
              replyChannel.Reply ()
              return! loop registeredTests
            | EndManager -> ()
          }
        loop [||]
      )
    { mailbox = testManager }

  member this.Register test =
    (fun r -> Register(test, r)) |> this.mailbox.PostAndReply

  member this.RunTests () =
   RunTests |> this.mailbox.PostAndReply

  member this.EndManager () =
    EndManager |> this.mailbox.Post
