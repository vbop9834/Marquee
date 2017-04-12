# marquee
## What is marquee?
marquee is an open source, Selenium based, UI Automation testing framework. Inspired by [lefthandedgoat's](https://github.com/lefthandedgoat) excellent, C# compatible library [canopy](https://github.com/lefthandedgoat/canopy).

Below is a marquee test suite program
```
open marquee
//This loads the Browser type

open testManager
//This loads all types associated with the concurrent TestManager

//Specify the AmountOfBrowsers configuration for concurrent execution
//MaximumBrowsersPossible is based on total registered tests
//other option is the following IWantThisManyBrowsers 3
let amountOfBrowsers = MaximumBrowsersPossible
//let amountOfBrowsers = IWantThisManyBrowsers 10

//resultsFunction is a hook into the test results
//type TestResultsFunction = ExecutionTime -> TestResultPackages -> int
let testResultsFunction = consoleReporter.resultsFunction

//a TestManager handles test registration, test execution, and test results
//we can start a testManager instance by calling TestManager.Create
//TestManager.Create has the function signature
//TestResultsFunction -> int -> BrowserType -> TestManager
//the int is to specify the amount of maximum browsers you want
//BrowserTypes are specified in the documentation found in the README file
//This example uses the BrowserType Chrome(driverDirectory)
//info on __SOURCE_DIRECTORY__ can be found here https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/source-line-file-path-identifiers
let testManager = Chrome __SOURCE_DIRECTORY__ |> TestManager.Create testResultsFunction amountOfBrowsers

//optional
//Define an operator for test registration
let (--) testDescription testFunc = testManager.Register testDescription testFunc
//alternative is to register tests like
//testManager.Register "If some action occurs something specific should happen" fun browser ->
//  browser.Url "www.eff.org"

//Test Registration using the -- operator defined above
"Button should click" -- fun browser ->
  browser.Url "http://lefthandedgoat.github.io/canopy/testpages/"
  "#button_clicked" |> browser.ElementTextEquals "button not clicked"
  browser.Click "#button"
  "#button_clicked" |> browser.ElementTextEquals "button clicked"
  browser.Displayed "#welcome"
  "#welcome" |> browser.ElementTextEquals "Welcome"

//Start execution of registered tests
testManager.RunTests ()
//Get exit code from test session and report the results via the TestResultsFunction
let exitCode = testManager.ReportResults ()
//End the Manager instance
testManager.EndManager ()
//exit the program and return the exitCode from the test results
exit exitCode
```

## LICENSE
[MIT](/LICENSE)

