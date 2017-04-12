module marquee.tests

open marquee
open testManager

let amountOfBrowsers = 10
let resultsFunction = fun results ->
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
  let testExitCodes = 
    results 
    |> List.map printResultsToScreen
  printfn "Tests Executed - %i" <| List.length results
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
let firstNameSelector = "#firstName"
let lastNameSelector = "#lastName"
let tableSelector = "#value_list td"
let itemListSelector = "#item_list option"
let checkboxSelector = "#checkbox"

"Navigating to the test page and clicking a button should change the button text" -- fun browser ->
  browser.Url testPageUrl
  buttonClickedSelector |> browser.ElementTextEquals "button not clicked"
  browser.Click buttonSelector
  buttonClickedSelector |> browser.ElementTextEquals "button clicked"
  browser.Displayed welcomeSelector
  welcomeSelector |> browser.ElementTextEquals "Welcome"

"On the test page, there should be a first and last name" -- fun browser ->
  browser.Url testPageUrl
  firstNameSelector |> browser.ElementTextEquals "John"
  lastNameSelector |> browser.ElementTextEquals "Doe"

"Should be able to clear first name input" -- fun browser ->
  browser.Url testPageUrl
  firstNameSelector |> browser.ClearInput
  firstNameSelector |> browser.ElementTextEquals ""

"Should be able to set last name input" -- fun browser ->
  browser.Url testPageUrl
  lastNameSelector |> browser.ClearInput
  lastNameSelector |> browser.ElementTextEquals ""
  lastNameSelector |> browser.SetInput "Smith"
  lastNameSelector |> browser.ElementTextEquals "Smith"

"Should be able to identify an item in a table" -- fun browser ->
  browser.Url testPageUrl
  tableSelector |> browser.TextExistsInElements "Value 1"
  tableSelector |> browser.TextExistsInElements "Value 2"
  tableSelector |> browser.TextExistsInElements "Value 3"
  tableSelector |> browser.TextExistsInElements "Value 4"
  tableSelector |> browser.TextExistsInElements "Value 4"

"Should be able to identify an item in a select list" -- fun browser ->
  browser.Url testPageUrl
  itemListSelector |> browser.TextExistsInElements "Item 1"
  itemListSelector |> browser.TextExistsInElements "Item 2"
  itemListSelector |> browser.TextExistsInElements "Item 3"
  itemListSelector |> browser.TextExistsInElements "Item 4"

"Should be able to check a checkbox" -- fun browser ->
  browser.Url testPageUrl
  checkboxSelector |> browser.AreElementsUnChecked
  checkboxSelector |> browser.CheckElements
  checkboxSelector |> browser.AreElementsChecked

"Should be able to uncheck a checkbox" -- fun browser ->
  browser.Url testPageUrl
  checkboxSelector |> browser.AreElementsUnChecked
  checkboxSelector |> browser.CheckElements
  checkboxSelector |> browser.AreElementsChecked
  checkboxSelector |> browser.UnCheckElements
  checkboxSelector |> browser.AreElementsUnChecked

testManager.RunTests ()
let exitCode = testManager.ReportResults ()
testManager.EndManager ()
exit exitCode
