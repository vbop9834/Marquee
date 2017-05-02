module Marquee.Tests

open Marquee
open TestManager

let testManagerConfiguration : TestManagerConfiguration =
  {
    TestResultsFunction = ConsoleReporter.resultsFunction
    AmountOfBrowsers = IWantThisManyBrowsers 5
    BrowserType = Chrome(CurrentDirectory)
    AssertionTimeout = 5000
    ElementTimeout = 5000
  }
let testManager = testManagerConfiguration |> TestManager.Create
let (--) testDescription testFunc = testManager.Register testDescription testFunc

//Helpers
let testPageUrl = "http://lefthandedgoat.github.io/canopy/testpages"
let alertTestPageUrl = sprintf "%s/alert" testPageUrl
let buttonClickedSelector = "#button_clicked"
let buttonSelector = "#button"
let welcomeSelector = "#welcome"
let firstNameSelector = "#firstName"
let lastNameSelector = "#lastName"
let tableSelector = "#value_list td"
let itemListSelector = "#item_list option"
let checkboxSelector = "#checkbox"
let radioOneSelector = "#radio1"
let radioTwoSelector = "#radio2"
let statesListSelector = "#states"
let hyperlinkSelector = "#hyperlink"
let hyperlinkClickedSelector = "#link_clicked"
let alertSelector = "#alert"
let confirmationAlertSelector = "#confirmation_test"

"Should be able to check if an element is displayed and assert the text" -- fun browser ->
  browser.Url testPageUrl
  browser.Displayed welcomeSelector
  welcomeSelector |> browser.ElementTextEquals "Welcome"

"Should be able to click a button which should change the button text" -- fun browser ->
  browser.Url testPageUrl
  buttonClickedSelector |> browser.ElementTextEquals "button not clicked"
  browser.Click buttonSelector
  buttonClickedSelector |> browser.ElementTextEquals "button clicked"

"Should be able to click a link which should change the link text" -- fun browser ->
  browser.Url testPageUrl
  hyperlinkClickedSelector |> browser.ElementTextEquals "link not clicked"
  browser.Click hyperlinkSelector
  hyperlinkClickedSelector |> browser.ElementTextEquals "link clicked"

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

"Should be able to check a radio button" -- fun browser ->
  browser.Url testPageUrl
  //Check radios are unchecked
  radioOneSelector |> browser.AreElementsUnChecked
  radioTwoSelector |> browser.AreElementsUnChecked
  //Click radio one
  radioOneSelector |> browser.Click
  //Check radio one is selected and two is not
  radioOneSelector |> browser.AreElementsChecked
  radioTwoSelector |> browser.AreElementsUnChecked
  //Click radio two
  radioTwoSelector |> browser.Click
  //Check radio two is selected and one is not
  radioTwoSelector |> browser.AreElementsChecked
  radioOneSelector |> browser.AreElementsUnChecked

"Should be able to select an item in a select list" -- fun browser ->
  browser.Url testPageUrl
  statesListSelector |> browser.IsOptionSelected "Select"
  statesListSelector |> browser.SetSelectOption "Kingman Reef"
  statesListSelector |> browser.IsOptionSelected "Kingman Reef"

"Should be able to dismiss an alert" -- fun browser ->
  browser.Url testPageUrl
  browser.Click alertSelector
  browser.AlertTextEquals "test!"
  browser.DismissAlert()

"Should be able to accept an alert" -- fun browser ->
  browser.Url alertTestPageUrl
  browser.Click confirmationAlertSelector
  browser.AlertTextEquals "Confirmation Test"
  browser.AcceptAlert()

testManager.RunTests ()
let exitCode = testManager.ReportResults ()
testManager.EndManager ()
exit exitCode
