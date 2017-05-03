/// ## Marquee
/// Marquee is a Selenium based UI Automation library. Inspired by [lefthandedgoat's excellent, C# compatible library - canopy]().
module Marquee
  open OpenQA.Selenium

  //## Types
  //### BrowserDirectoryOption
  //The BrowserDirectoryOption type is used for specifying the location of a web driver
  //To specify a specific location use the following syntax
  //SpecificDirectory "C:\webdrivers"
  //Or use the CurrentDirectory option
  type BrowserDirectoryOption =
  | CurrentDirectory
  | SpecificDirectory of string

  //### BrowserType
  //BrowserType is used to define what browser the tests should use
  //Current Options are Chrome, Firefox, or PhantomJs
  //The BrowserType takes a BrowserDirectoryOption
  //Use the syntax
  //Chrome(CurrentDirectory)
  //or
  //Chrome(SpecificDirectory "C:\webdrivers")
  type BrowserType =
    | Chrome of BrowserDirectoryOption
    | Firefox of BrowserDirectoryOption
    | PhantomJs of BrowserDirectoryOption

  type private WaitResult<'T> =
  | WaitSuccessful of 'T
  | WaitFailure of System.Exception

  type private ContinueFunction<'T> = unit -> WaitResult<'T>
  type WebElements = IWebElement array

  exception UnableToFindElementsWithSelectorException of string
  exception NoWebElementsDisplayedException of string
  exception WebElementsDoNotMatchSuppliedTextException of string
  exception WebElementIsReadOnlyException of IWebElement
  exception WebElementSelectDoesNotContainTextException of string
  exception WebElementIsNotCheckedException of IWebElement
  exception WebElementIsCheckedException of IWebElement
  exception NoOptionInSelectThatMatchesTextException of string
  exception OptionIsNotSelectedException of string
  exception AlertTextDoesNotEqualException of string

  let private wait (timeout : int) (continueFunction : ContinueFunction<'T>) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let rec testContinueFunction lastActivationTime =
      let elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
      match (elapsedMilliseconds - lastActivationTime) >= 1000.0 with
      | true ->
          match continueFunction() with
          | WaitSuccessful result ->
            result
          | WaitFailure ex ->
            let lastActivationTime = stopwatch.Elapsed.TotalMilliseconds
            match elapsedMilliseconds - (float timeout) >= 0.001 with
            | true ->
              raise ex
            | false ->
              testContinueFunction lastActivationTime
      | false ->
        testContinueFunction lastActivationTime
    testContinueFunction -1000.0

  //### BrowserConfiguration
  //The BrowserConfiguration type is a record that is passed to the Browser.Create function
  //For example
  (*
      let browserConfiguration : BrowserConfiguration =
        {
          BrowserType = Chrome(CurrentDirectory)
          ElementTimeout = 5000
          AssertionTimeout = 5000
        }
   *)
  //#### BrowserType
  //BrowserType in the BrowserConfiguration record sets what browser the webdriver should launch
  //#### ElementTimeout
  //The ElementTimeout in the BrowserConfiguration record is an integer that sets the amount of milliseconds that a Marquee action takes before giving up.
  //This is useful when dealing with web content that is slow to update on the page
  //#### AssertionTimeout
  //The AssertionTimeout in the BrowserConfiguration record is an integer that sets the amount of milliseconds that a Marquee assertion takes before giving upd.
  //This is useful when dealing with web content that is slow to update on the page
  type BrowserConfiguration =
    {
      BrowserType : BrowserType
      ElementTimeout : int
      AssertionTimeout : int
    }

  //### Browser
  //Browser is the main record type for Browser manipulation in Marquee
  type Browser =
    {
      Instance : OpenQA.Selenium.IWebDriver
      ElementTimeout : int
      AssertionTimeout : int
    }

    //#### Create
    //The Create function is used to create a live browser instance
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    browserConfiguration |> Browser.Create
     *)
    static member Create (configuration : BrowserConfiguration) : Browser =
      let getBrowserDirectory directoryOption =
        match directoryOption with
        | CurrentDirectory -> System.IO.Directory.GetCurrentDirectory()
        | SpecificDirectory dir -> dir
      let hideCommandPromptWindow = true
      let browserInstance =
        match configuration.BrowserType with
        | Chrome chromeDir ->
          let chromeDir = getBrowserDirectory chromeDir
          let chromeDriverService =
            let service = Chrome.ChromeDriverService.CreateDefaultService(chromeDir)
            service.HideCommandPromptWindow <- hideCommandPromptWindow
            service
          let options = Chrome.ChromeOptions()
          options.AddArgument("--disable-extensions")
          options.AddArgument("disable-infobars")
          options.AddArgument("test-type") //https://code.google.com/p/chromedriver/issues/detail?id=799
          new OpenQA.Selenium.Chrome.ChromeDriver(chromeDriverService , options) :> IWebDriver
        | Firefox firefoxDir ->
          let firefoxDir = getBrowserDirectory firefoxDir
          let firefoxService =
            let service = OpenQA.Selenium.Firefox.FirefoxDriverService.CreateDefaultService(firefoxDir)
            service.HideCommandPromptWindow <- hideCommandPromptWindow
            service
          new OpenQA.Selenium.Firefox.FirefoxDriver(firefoxService) :> IWebDriver
        | PhantomJs phantomDir ->
          let phantomDir = getBrowserDirectory phantomDir
          let phantomJsDriverService =
            let service = PhantomJS.PhantomJSDriverService.CreateDefaultService(phantomDir)
            service.HideCommandPromptWindow <- hideCommandPromptWindow
            service
          new OpenQA.Selenium.PhantomJS.PhantomJSDriver(phantomJsDriverService) :> IWebDriver
      {
        Instance = browserInstance
        ElementTimeout = configuration.ElementTimeout
        AssertionTimeout = configuration.AssertionTimeout
       }

    //#### Quit
    //Quit is used to end the live browser instance
    //When using the built in TestManager this function isn't necessary to call
    member this.Quit () =
      this.Instance.Quit()

    member private this.WaitForAssertion assertionFunction =
      let waitForAssertion timeout assertionFunc =
        let continueFunction : ContinueFunction<unit> = fun () ->
          try
            assertionFunc ()
            WaitSuccessful ()
          with
          | ex -> WaitFailure ex
        wait timeout continueFunction
      waitForAssertion this.AssertionTimeout assertionFunction

    //#### FindElements
    //FindElements searches the current page the browser is on for web elements that match the provided CSS selector
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".some_class"
    browser.FindElements cssSelector
    *)
    member this.FindElements cssSelector =
      let searchContext : ISearchContext = this.Instance :> ISearchContext
      let findElementsByCssSelector timeout cssSelector (browser : ISearchContext) =
        let continueFunction : ContinueFunction<WebElements> =
          fun _ ->
            let elements = browser.FindElements((By.CssSelector cssSelector)) |> Seq.toArray
            match elements with
            | [||] -> WaitFailure <| UnableToFindElementsWithSelectorException cssSelector
            | elements ->
              WaitSuccessful elements
        let elements =
          wait timeout continueFunction
        elements
      let elements = findElementsByCssSelector this.ElementTimeout cssSelector searchContext
      elements

    //#### Click
    //The Click function clicks all web elements that match the provided CSS Selector
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".some_button"
    browser.Click cssSelector
     *)
    member this.Click cssSelector =
      let elements = this.FindElements cssSelector
      elements |> Array.iter(fun element -> element.Click())

    //#### Displayed
    //The Displayed function asserts that some element that matches the provided CSS selector is displayed
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".welcome_text"
    browser.Displayed cssSelector
     *)
    member this.Displayed cssSelector =
      let isShown (element : IWebElement) =
        let opacity = element.GetCssValue("opacity")
        let display = element.GetCssValue("display")
        display <> "none" && opacity = "1"
      let assertionFunction = fun () ->
        let elements = this.FindElements cssSelector
        match elements |> Array.filter(isShown) with
        | [||] -> raise <| NoWebElementsDisplayedException cssSelector
        | _ -> ()
      this.WaitForAssertion assertionFunction

    //#### Url
    //The Url function navigates the browser to the supplied url string
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    browser.Url "http://www.github.com"
     *)
    member this.Url (url : string) =
      this.Instance.Navigate().GoToUrl(url)

    static member private ReadText (element : IWebElement) =
        match element.TagName.ToLower() with
        | "input" | "textarea" -> element.GetAttribute("value")
        | _ ->
          element.Text

    //#### ElementTextEquals
    //The ElementTextEquals function asserts that all elements that match the provided cssSelector contains some text
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".welcome_message"
    "Welcome to the website!" |> browser.ElementTextEquals cssSelector
    *)
    member this.ElementTextEquals testText cssSelector =
      let elements = this.FindElements cssSelector
      //TODO replace filter with map filter operation
      //for efficiency
      let assertionFunction = fun () ->
        match elements |> Array.filter(fun element -> (Browser.ReadText element) <> testText) with
        | [||] -> ()
        | elements ->
          let elements = elements |> Array.map(Browser.ReadText)
          raise <| WebElementsDoNotMatchSuppliedTextException (sprintf "%s is not found in %A" testText elements)
      this.WaitForAssertion assertionFunction

    //#### ClearInput
    //The ClearInput function clears the text input of all web elements that match the provided CSS selector
    //For example
    (*
    let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".username"
    browser.ClearInput cssSelector
    *)
    member this.ClearInput cssSelector =
      let elements = this.FindElements cssSelector
      let clear (element : IWebElement) =
        match element.GetAttribute("readonly") with
        | "true" -> raise <| WebElementIsReadOnlyException element
        | _ -> element.Clear()
      elements |> Array.iter clear

    //#### SetInput
    //The SetInput function sets the input text of all web elements that match the provided CSS selector
    (*
    let browserConfiguration : BrowserConfiguration =
       {
         BrowserType = Chrome(CurrentDirectory)
         ElementTimeout = 5000
         AssertionTimeout = 5000
       }
    let browser = browserConfiguration |> Browser.Create
    let cssSelector = ".username"
    "JohnDoe" |> browser.SetInput cssSelector
     *)
    member this.SetInput text cssSelector =
      let elements = this.FindElements cssSelector
      let clear (element : IWebElement) =
        match element.GetAttribute("readonly") with
        | "true" -> raise <| WebElementIsReadOnlyException element
        | _ ->
          element.Clear()
          element.SendKeys(text)
      elements |> Array.iter clear

    //#### TestExistsInElements
    //The TestExistsInElements function asserts that at least one web element that matches the provided CSS selector also matches the supplied text
    //This is different than ElementTextEquals because ElementTextEquals asserts that all elements match the provided text
    //ElementTextEquals should be used when the CSS selector is specific to one type of web element
    //TestExistsInElements should be used when the CSS selector is general and may gather multiple web elements

    //For example
      (*
      let browserConfiguration : BrowserConfiguration =
        {
          BrowserType = Chrome(CurrentDirectory)
          ElementTimeout = 5000
          AssertionTimeout = 5000
        }
      let browser = browserConfiguration |> Browser.Create
      let cssSelector = "div"
      "Welcome to the website!" |> browser.TextExistsInElements cssSelector
    *)
    member this.TextExistsInElements text cssSelector =
      let assertionFunction = fun () ->
        let elements = this.FindElements cssSelector
        let testElementText element =
          (Browser.ReadText element) <> text
        match elements  |> Array.tryFind testElementText with
        | Some _ -> ()
        | None -> raise <| WebElementSelectDoesNotContainTextException text
      this.WaitForAssertion assertionFunction

    //#### CheckElements
    //The CheckElements function performs the check action on a checkbox web element
    //For example
    (*
      let browserConfiguration : BrowserConfiguration =
           {
             BrowserType = Chrome(CurrentDirectory)
             ElementTimeout = 5000
             AssertionTimeout = 5000
           }
      let browser = browserConfiguration |> Browser.Create
      let cssSelector = ".some_checkbox"
      browser.CheckElements cssSelector
     *)
    member this.CheckElements cssSelector =
      let elements = this.FindElements cssSelector
      let check (element : IWebElement) =
        match element.Selected with
        | true -> ()
        | false -> element.Click()
      elements |> Array.iter check

    //#### UnCheckElements
    //The UnCheckElements function performs the uncheck action on a checkbox web element
    //For example
    (*
      let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
      let browser = browserConfiguration |> Browser.Create
      let cssSelector = ".some_checkbox"
      browser.UnCheckElements cssSelector
     *)
    member this.UnCheckElements cssSelector =
      let elements = this.FindElements cssSelector
      let check (element : IWebElement) =
        match element.Selected with
        | true -> element.Click()
        | false -> ()
      elements |> Array.iter check

    //#### AreElementsChecked
    //The AreElementsChecked function asserts that all web elements gathered by the provided CSS selector are checked
    //For example
    (*
      let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
      let browser = browserConfiguration |> Browser.Create
      let cssSelector = ".some_checkbox"
      browser.AreElementsChecked cssSelector
     *)
    member this.AreElementsChecked cssSelector =
      let assertionFunction = fun () ->
        let elements = this.FindElements cssSelector
        let testElement (element : IWebElement) =
          match element.Selected with
          | true -> ()
          | false -> raise <| WebElementIsNotCheckedException element
        elements |> Array.iter testElement
      this.WaitForAssertion assertionFunction

    //#### AreElementsUnChecked
    //The AreElementsUnChecked function asserts that all web elements gathered by the provided CSS selector are unchecked
    //For example
    (*
      let browserConfiguration : BrowserConfiguration =
      {
        BrowserType = Chrome(CurrentDirectory)
        ElementTimeout = 5000
        AssertionTimeout = 5000
      }
      let browser = browserConfiguration |> Browser.Create
      let cssSelector = ".some_checkbox"
      browser.AreElementsUnChecked cssSelector
     *)
    member this.AreElementsUnChecked cssSelector =
      let assertionFunction = fun () ->
        let elements = this.FindElements cssSelector
        let testElement (element : IWebElement) =
          match element.Selected with
          | true -> raise <| WebElementIsCheckedException element
          | false -> ()
        elements |> Array.iter testElement
      this.WaitForAssertion assertionFunction

    member this.SetSelectOption option selectCssSelector =
      let cssSelector = sprintf "%s option" selectCssSelector
      let elements =
        this.FindElements cssSelector
        |> Array.filter(fun element -> element.Text = option)
      match elements with
      | [||] -> raise <| NoOptionInSelectThatMatchesTextException option
      | elements -> elements |> Array.iter(fun element -> element.Click())

    member this.IsOptionSelected option selectCssSelector =
      let assertionFunction = fun () ->
        let cssSelector = sprintf "%s option" selectCssSelector
        let elements =
          this.FindElements cssSelector
        match elements |> Array.tryFind(fun element -> element.Text = option) with
        | None -> raise <| NoOptionInSelectThatMatchesTextException option
        | Some element ->
          match element.Selected with
          | true -> ()
          | false -> raise <| OptionIsNotSelectedException option
      this.WaitForAssertion assertionFunction

    member this.AlertTextEquals text  =
      let assertionFunction = fun () ->
        let alert = this.Instance.SwitchTo().Alert()
        match alert.Text = text with
        | true -> ()
        | false -> raise <| AlertTextDoesNotEqualException text
      this.WaitForAssertion assertionFunction

    member this.AcceptAlert () =
      let alert = this.Instance.SwitchTo().Alert()
      alert.Accept()

    member this.DismissAlert () =
      let alert = this.Instance.SwitchTo().Alert()
      alert.Dismiss()
