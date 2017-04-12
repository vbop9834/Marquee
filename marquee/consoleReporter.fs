module consoleReporter
  open testManager

  let resultsFunction : TestResultsFunction = fun executionTime results ->
    printfn "Test Results"
    printfn "----------------------------------"
    let printResultsToScreen (testDescription, testResult) =
      printfn "%s" testDescription
      match testResult with
      | TestPassed -> 
        printfn "Passed"
        0
      | TestFailed ex -> 
        printfn "Failed"
        printfn "%s" ex.Message
        printfn "%s" ex.StackTrace
        1
    let numberOfTests = (List.length results) 
    let numberOfFailedTests = results |> List.filter (fun (_, result) -> result <> TestPassed) |> List.length
    let numberOfPassedTests = numberOfTests - numberOfFailedTests
    let testExitCodes = 
      results 
      |> List.map printResultsToScreen
    printfn "----------------------------------"
    printfn "%i Tests Executed in %f seconds" numberOfTests executionTime.TotalSeconds
    printfn "%i Passed" numberOfPassedTests
    printfn "%i Failed" numberOfFailedTests
    System.Console.ReadLine() |> ignore
    match testExitCodes |> List.exists(fun exitCode -> exitCode = 1) with
    | true -> 1
    | false -> 0
