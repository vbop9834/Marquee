module marquee
  open OpenQA.Selenium

  type WaitResult<'T> =
  | WaitSuccessful of 'T
  | WaitFailure

  type ContinueFunction<'T> = unit -> WaitResult<'T>
  type WebElements = IWebElement array

  exception WaitTimeoutException
  exception NoWebElementsDisplayed of string
  exception WebElementsDoNotMatchSuppliedText of string
  exception WebElementIsReadOnly of IWebElement
  exception WebElementSelectDoesNotContainText of string
  exception WebElementIsNotChecked of IWebElement
  exception WebElementIsChecked of IWebElement

  let private wait (timeout : int) (continueFunction : ContinueFunction<'T>) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let rec testContinueFunction lastActivationTime =
      let elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
      match elapsedMilliseconds - (float timeout) >= 0.001 with
      | true ->
        raise WaitTimeoutException
      | false ->
        match (elapsedMilliseconds - lastActivationTime) >= 1000.0 with
        | true ->
          match continueFunction() with
          | WaitSuccessful result ->
            result
          | WaitFailure ->
            let lastActivationTime = stopwatch.Elapsed.TotalMilliseconds
            testContinueFunction lastActivationTime
        | false ->
          testContinueFunction lastActivationTime
    testContinueFunction -1000.0

  let private findElementsByCssSelector timeout cssSelector (browser : ISearchContext) =
    try
      let continueFunction : ContinueFunction<WebElements> =
        fun _ ->
          let elements = browser.FindElements((By.CssSelector cssSelector)) |> Seq.toArray
          match elements with
          | [||] -> WaitFailure
          | elements -> 
            WaitSuccessful elements
      let elements =
        wait timeout continueFunction
      elements
    with | ex -> Array.empty

  type BrowserType =
    | Chrome of string
    | Firefox of string

  type Browser =
    {
      instance : OpenQA.Selenium.IWebDriver
      elementTimeout : int
    }

    static member Create (browserType : BrowserType) : Browser =
      let hideCommandPromptWindow = true
      let browserInstance =
        match browserType with
        | Chrome chromeDir ->
          let chromeDriverService _ =
            let service = Chrome.ChromeDriverService.CreateDefaultService(chromeDir)
            service.HideCommandPromptWindow <- hideCommandPromptWindow
            service
          let options = Chrome.ChromeOptions()
          options.AddArgument("--disable-extensions")
          options.AddArgument("disable-infobars")
          options.AddArgument("test-type") //https://code.google.com/p/chromedriver/issues/detail?id=799
          new OpenQA.Selenium.Chrome.ChromeDriver(chromeDriverService (), options) :> IWebDriver
        | Firefox firefoxDir ->
          let options = new OpenQA.Selenium.Firefox.FirefoxOptions()
          options.BrowserExecutableLocation <- firefoxDir
          new OpenQA.Selenium.Firefox.FirefoxDriver(options) :> IWebDriver
      { instance = browserInstance; elementTimeout = 5000 }

    member this.FindElements cssSelector =
      let searchContext : ISearchContext = this.instance :> ISearchContext
      let elements = findElementsByCssSelector this.elementTimeout cssSelector searchContext
      elements

    member this.Click cssSelector =
      let elements = this.FindElements cssSelector
      elements |> Array.iter(fun element -> element.Click())

    member this.Displayed cssSelector =
      let isShown (element : IWebElement) =
        let opacity = element.GetCssValue("opacity")
        let display = element.GetCssValue("display")
        display <> "none" && opacity = "1"
      let elements = this.FindElements cssSelector
      match elements |> Array.filter(isShown) with
      | [||] -> raise <| NoWebElementsDisplayed cssSelector
      | itemsDisplayed -> () 

    member this.Url (url : string) =
      this.instance.Navigate().GoToUrl(url)

    static member private ReadText (element : IWebElement) =
        match element.TagName.ToLower() with
        | "input" | "textarea" -> element.GetAttribute("value")
        | _ -> 
          element.Text

    member this.ElementTextEquals testText cssSelector =
      let elements = this.FindElements cssSelector
      //TODO replace filter with map filter operation
      //for efficiency
      match elements |> Array.filter(fun element -> (Browser.ReadText element) <> testText) with
      | [||] -> () 
      | elements -> 
        let elements = elements |> Array.map(fun element -> Browser.ReadText element)
        raise <| WebElementsDoNotMatchSuppliedText (sprintf "%s is not found in %A" testText elements)

    member this.ClearInput cssSelector =
      let elements = this.FindElements cssSelector
      let clear (element : IWebElement) =
        match element.GetAttribute("readonly") with
        | "true" -> raise <| WebElementIsReadOnly element
        | _ -> element.Clear()
      elements |> Array.iter clear

    member this.SetInput text cssSelector =
      let elements = this.FindElements cssSelector
      let clear (element : IWebElement) =
        match element.GetAttribute("readonly") with
        | "true" -> raise <| WebElementIsReadOnly element
        | _ -> 
          element.Clear()
          element.SendKeys(text)
      elements |> Array.iter clear

    member this.TextExistsInElements text cssSelector =
      let elements = this.FindElements cssSelector
      let testElementText element =
        (Browser.ReadText element) <> text
      match elements  |> Array.tryFind testElementText with 
      | Some elementThatContainsText -> ()
      | None -> raise <| WebElementSelectDoesNotContainText text 
      
    member this.CheckElements cssSelector =
      let elements = this.FindElements cssSelector
      let check (element : IWebElement) =
        match element.Selected with
        | true -> ()
        | false -> element.Click()
      elements |> Array.iter check

    member this.UnCheckElements cssSelector =
      let elements = this.FindElements cssSelector
      let check (element : IWebElement) =
        match element.Selected with
        | true -> element.Click()
        | false -> ()
      elements |> Array.iter check

    member this.AreElementsChecked cssSelector =
      let elements = this.FindElements cssSelector
      let testElement (element : IWebElement) =
        match element.Selected with
        | true -> ()
        | false -> raise <| WebElementIsNotChecked element
      elements |> Array.iter testElement

    member this.AreElementsUnChecked cssSelector =
      let elements = this.FindElements cssSelector
      let testElement (element : IWebElement) =
        match element.Selected with
        | true -> raise <| WebElementIsChecked element
        | false -> ()
      elements |> Array.iter testElement
