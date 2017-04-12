module marquee.tests

open marquee
open testManager

let amountOfBrowsers = 5
let resultsFunction = fun results ->
  let printResultsToScreen (testDescription, testResult) =
    printfn "%s" testDescription
    match testResult with
    | TestPassed -> 
      printfn "Passed"
      0
    | TestFailed ex -> 
      printfn "Failed"
      printfn "%s" ex.StackTrace
      1
  let testExitCodes = 
    results 
    |> List.map printResultsToScreen
  System.Console.ReadLine() |> ignore
  match testExitCodes |> List.exists(fun exitCode -> exitCode = 1) with
  | true -> 1
  | false -> 0
let testManager = Chrome __SOURCE_DIRECTORY__ |> TestManager.Create resultsFunction amountOfBrowsers
let (--) testDescription testFunc = testManager.Register (testDescription, testFunc)

//Helpers
let testPageUrl = "http://lefthandedgoat.github.io/canopy/testpages/"
let buttonClickedSelector = "#button_clicked"
let buttonSelector = "#button"
let welcomeSelector = "#welcome"

"Navigating to the test page and clicking a button should change the button text" -- fun browser ->
  browser.Url testPageUrl
  buttonClickedSelector |> browser.ElementTextEquals "button not clicked"
  browser.Click buttonSelector
  buttonClickedSelector |> browser.ElementTextEquals "button clicked"
  browser.Displayed welcomeSelector
  welcomeSelector |> browser.ElementTextEquals "Welcome"

testManager.RunTests ()
let exitCode = testManager.ReportResults ()
testManager.EndManager ()
exit exitCode
