(**
# marquee test suite
The marquee test suite is broken down into the following components.
- open marquee and testManager dependencies
- specify the maximum amount of browsers that are able to run concurrently
- specify the function for reporting test results
- create the test manager
- register tests
- tell the test manager to run the tests
- end the test manager
- exit the program
*)

#I __SOURCE_DIRECTORY__
#I "./packages/Selenium.WebDriver/lib/net40"
#I "./marquee"

#r "WebDriver.dll"
#load "marquee.fs"
#load "testManager.fs"
#load "consoleReporter.fs"

(**
## Dependencies
The marquee module loads the crucial BrowserType for specifying the Browser
*)
open marquee

//The testManager module loads test registration and concurrent execution types
//As well as the TestResultsFunction for handling test results
open testManager

(**
## Configurations
Specify the AmountOfBrowsers configuration for concurrent execution
MaximumBrowsersPossible is based on total registered tests
other option is the following IWantThisManyBrowsers 3
*)
let amountOfBrowsers = MaximumBrowsersPossible
//Also possible is the IWantThisManyBrowsers(int) option
let specificAmountOfBrowsers = IWantThisManyBrowsers 10

(**
### Browser
Currently supported browsers are
- Chrome
- Firefox
*)
let chromeBrowser = Chrome(CurrentDirectory)
let firefoxBrowser = SpecificDirectory __SOURCE_DIRECTORY__ |> Firefox

(**
### TestResultsFunction 
The TestResultsFunction is used for configuring how test results are reported or saved

consoleReporter is a module provided by marquee for reporting via the console
*)
let testResultsFunction = consoleReporter.resultsFunction

(** 
## TestManager
A TestManager handles test registration, test execution, and test results

We can start a testManager instance by calling TestManager.Create

TestManager.Create has the function signature
TestManagerConfiguration -> TestManager
*)
let testManagerConfiguration : TestManagerConfiguration =
  {
    BrowserType = chromeBrowser
    TestResultsFunction = testResultsFunction
    AmountOfBrowsers = amountOfBrowsers
    AssertionTimeout = 5000
    ElementTimeout = 5000
  }
let testManager = testManagerConfiguration |> TestManager.Create

(**
### Optional Test Registration Operator
Define an operator for test registration
*)
let (--) testDescription testFunc = testManager.Register testDescription testFunc

(**
#### Test Registration
*)
//Describe what the test is testing for and start the test function
"Button should click" -- fun browser ->
//Use the supplied browser to navigate to a specific url
  browser.Url "http://lefthandedgoat.github.io/canopy/testpages/"
//Assert that elements gathered using the css selector have the test "button not clicked"
  "#button_clicked" |> browser.ElementTextEquals "button not clicked"
//Click the elements gathered with the css selector #button
  browser.Click "#button"
//Assert that elements gathered using the css selector have the test "button clicked"
  "#button_clicked" |> browser.ElementTextEquals "button clicked"
//Assert that an element that matches the css selector #welcome is displayed
  browser.Displayed "#welcome"
//Assert that elements gathered using the css selector have the text "Welcome"
  "#welcome" |> browser.ElementTextEquals "Welcome"

(**
alternative is to register tests using the following
```
testManager.Register "If some action occurs something specific should happen" fun browser ->
  browser.Url "www.eff.org"
```
*)


(**
### Executing Tests
Start concurrent execution of registered tests
*)
testManager.RunTests ()
//Report the results via the TestResultsFunction and get exit code from test session
let exitCode = testManager.ReportResults ()
//End the Manager instance
testManager.EndManager ()
//exit the program and return the exitCode from the test results
exit exitCode
