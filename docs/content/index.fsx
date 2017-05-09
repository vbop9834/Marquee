(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Marquee
======================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Marquee library can be <a href="https://nuget.org/packages/Marquee">installed from NuGet</a>:
      <pre>PM> Install-Package Marquee</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

## Marquee
Marquee is a Selenium based UI Automation library. Inspired by [lefthandedgoat's excellent, C# compatible library - canopy]().

*)
#r "Marquee.dll"
open Marquee

//## Types
//The following sections describe the various types that Marquee uses
//### BrowserDirectoryOption
//The BrowserDirectoryOption type is used for specifying the location of a web driver
//To specify a specific location use the following syntax
BrowserDirectoryOption.SpecificDirectory "C:\webdrivers"
//Or use the CurrentDirectory option
BrowserDirectoryOption.CurrentDirectory

//### BrowserType
//BrowserType is used to define what browser the tests should use
//Current Options are Chrome, Firefox, or PhantomJs
//The BrowserType takes a BrowserDirectoryOption
//Use the syntax
BrowserType.Chrome(CurrentDirectory)
//or
BrowserType.Chrome(SpecificDirectory "C:\webdrivers")

//### BrowserConfiguration
//The BrowserConfiguration type is a record that is passed to the Browser.Create function
//For example
let browserConfiguration : BrowserConfiguration =
  {
    BrowserType = Chrome(CurrentDirectory)
    ElementTimeout = 5000
    AssertionTimeout = 5000
  }

//#### BrowserType
//BrowserType in the BrowserConfiguration record sets what browser the webdriver should launch
//#### ElementTimeout
//The ElementTimeout in the BrowserConfiguration record is an integer that sets the amount of milliseconds that a Marquee action takes before giving up.
//This is useful when dealing with web content that is slow to update on the page
//#### AssertionTimeout
//The AssertionTimeout in the BrowserConfiguration record is an integer that sets the amount of milliseconds that a Marquee assertion takes before giving upd.
//This is useful when dealing with web content that is slow to update on the page

//### Browser
//Browser is the main record type for Browser manipulation in Marquee
//#### Create
//The Create function is used to create a live browser instance
//For example
let browserConfiguration : BrowserConfiguration =
  {
    BrowserType = Chrome(CurrentDirectory)
    ElementTimeout = 5000
    AssertionTimeout = 5000
  }
browserConfiguration |> Browser.Create

//#### Url
//The Url function navigates the browser to the supplied url string
//For example
browser.Url "http://www.github.com"

//#### Quit
//Quit is used to end the live browser instance
//When using the built in TestManager this function isn't necessary to call
browser.Quit()


//#### FindElements
//FindElements searches the current page the browser is on for web elements that match the provided CSS selector
//For example
let cssSelector = ".some_class"
browser.FindElements cssSelector

//#### Click
//The Click function clicks all web elements that match the provided CSS Selector
//For example
let cssSelector = ".some_button"
browser.Click cssSelector

//#### Displayed
//The Displayed function asserts that some element that matches the provided CSS selector is displayed
//For example
let cssSelector = ".welcome_text"
browser.Displayed cssSelector

//#### ElementTextEquals
//The ElementTextEquals function asserts that all elements that match the provided cssSelector contains some text
//For example
let cssSelector = ".welcome_message"
"Welcome to the website!" |> browser.ElementTextEquals cssSelector

//#### ClearInput
//The ClearInput function clears the text input of all web elements that match the provided CSS selector
//For example
let cssSelector = ".username"
browser.ClearInput cssSelector

//#### SetInput
//The SetInput function sets the input text of all web elements that match the provided CSS selector
let cssSelector = ".username"
"JohnDoe" |> browser.SetInput cssSelector

//#### TestExistsInElements
//The TestExistsInElements function asserts that at least one web element that matches the provided CSS selector also matches the supplied text
//This is different than ElementTextEquals because ElementTextEquals asserts that all elements match the provided text
//ElementTextEquals should be used when the CSS selector is specific to one type of web element
//TestExistsInElements should be used when the CSS selector is general and may gather multiple web elements

//For example
let cssSelector = "div"
"Welcome to the website!" |> browser.TextExistsInElements cssSelector

//#### CheckElements
//The CheckElements function performs the check action on a checkbox web element
//For example
let cssSelector = ".some_checkbox"
browser.CheckElements cssSelector

//#### UnCheckElements
//The UnCheckElements function performs the uncheck action on a checkbox web element
//For example
let cssSelector = ".some_checkbox"
browser.UnCheckElements cssSelector

//#### AreElementsChecked
//The AreElementsChecked function asserts that all web elements gathered by the provided CSS selector are checked
//For example
let cssSelector = ".some_checkbox"
browser.AreElementsChecked cssSelector

//#### AreElementsUnChecked
//The AreElementsUnChecked function asserts that all web elements gathered by the provided CSS selector are unchecked
//For example
let cssSelector = ".some_checkbox"
browser.AreElementsUnChecked cssSelector

(**
#### SetSelectOption
The SetSelectOption function sets a select web element to the specified option

For example
Say we wanted to select Option One for the following html snippet
<select class="some_select">
  <option>Option One</option>
  <option>Option Two</option>
  <option>Option Three</option>
</select>
 **)
let cssSelector = ".some_select"
cssSelector |> browser.SetSelectOption "Option One"

(**
#### IsOptionSelected
The IsOptionSelected function asserts that the specified option in a select web element is selected
**)
let cssSelector = ".some_select"
cssSelector |> browser.SetSelectOption "Option One"
cssSelector |> browser.IsOptionSelected "Option One"

(**
#### AlertTextEquals
The AlertTextEquals function asserts that a browser alert has the supplied text
**)
browser.AlertTextEquals "Warning!"

(**
#### AcceptAlert
The AcceptAlert function initiates the accept action on a browser alert
**)
browser.AcceptAlert ()

(**
#### DismissAlert
The DismissAlert function initiates the dismiss action on a browser alert
**)
browser.DismissAlert ()

(**
 
Contributing and copyright
--------------------------

  [content]: https://github.com/fsprojects/Marquee/tree/master/docs/content
  [gh]: https://github.com/jeremybellows/Marquee
  [issues]: https://github.com/jeremybellows/Marquee/issues
  [readme]: https://github.com/jeremybellows/Marquee/blob/master/README.md
  [license]: https://github.com/jeremybellows/Marquee/blob/master/LICENSE.txt
*)
