module ConsoleReporter
  open TestManager

  let private writeLine (text: string) =
    System.Console.WriteLine text

  let private resetConsoleColor () = System.Console.ResetColor()
  let private setConsoleColor color =
    resetConsoleColor ()
    System.Console.ForegroundColor <- color

  let infoFunction : InfoFunction = fun info -> 
    setConsoleColor System.ConsoleColor.DarkGray
    writeLine <| sprintf "INFO - %s" info
    resetConsoleColor ()

  let resultsFunction : TestResultsFunction = fun executionTime results ->
    writeLine "Test Results"
    writeLine "----------------------------------"
    let printResultsToScreen (testDescription, testResult) =
      writeLine testDescription
      match testResult with
      | TestPassed ->
        setConsoleColor System.ConsoleColor.Green
        writeLine "Passed"
        resetConsoleColor ()
        0
      | TestFailed ex ->
        setConsoleColor System.ConsoleColor.Red
        writeLine "Failed"
        resetConsoleColor ()
        writeLine ex.Message
        writeLine ex.StackTrace
        1
    let numberOfTests = (List.length results)
    let numberOfFailedTests = results |> List.filter (fun (_, result) -> result <> TestPassed) |> List.length
    let numberOfPassedTests = numberOfTests - numberOfFailedTests
    let testExitCodes =
      results
      |> List.map printResultsToScreen
    writeLine "----------------------------------"
    writeLine <| sprintf "%i Tests executed in %f seconds" numberOfTests executionTime.TotalSeconds
    setConsoleColor System.ConsoleColor.Green
    writeLine <| sprintf "%i Passed" numberOfPassedTests
    setConsoleColor System.ConsoleColor.Red
    writeLine <| sprintf "%i Failed" numberOfFailedTests
    resetConsoleColor ()
    match testExitCodes |> List.exists(fun exitCode -> exitCode = 1) with
    | true -> 1
    | false -> 0
