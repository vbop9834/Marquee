module marquee
  open OpenQA.Selenium

  type WaitResult<'T> =
  | WaitSuccessful of 'T
  | WaitFailure

  type ContinueFunction<'T> = unit -> WaitResult<'T>
  type WebElements = IWebElement array

  exception WaitTimeoutException
  exception WebElementsAreDisplayed of WebElements

  let private wait (timeout : int) (continueFunction : ContinueFunction<'T>) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let rec testContinueFunction lastActivationTime =
      let elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds
      match System.BitConverter.DoubleToInt64Bits(elapsedMilliseconds) >= (int64 timeout) with
      | true ->
        raise WaitTimeoutException
      | false ->
        match (elapsedMilliseconds - lastActivationTime) >= 1000.0 with
        | true ->
          let lastActivationTime = stopwatch.Elapsed.TotalMilliseconds
          match continueFunction() with
          | WaitSuccessful result ->
            result
          | WaitFailure ->
            testContinueFunction lastActivationTime
        | false ->
          testContinueFunction lastActivationTime
    testContinueFunction -1000.0

  let private findElementsByCssSelector timeout cssSelector (browser : ISearchContext) =
    try
      let continueFunction : ContinueFunction<WebElements> =
        fun _ ->
          let elements = browser.FindElements(cssSelector) |> Seq.toArray
          match elements |> Array.isEmpty with
          | true -> WaitFailure
          | false -> WaitSuccessful elements
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

    member private this.FindElements cssSelector =
      let elements = findElementsByCssSelector this.elementTimeout cssSelector this.instance
      elements

    member this.Click cssSelector =
      let elements = this.FindElements cssSelector
      elements |> Array.iter(fun element -> element.Click())

    member this.Displayed cssSelector =
      let isShown (element : IWebElement) =
        let opacity = element.GetCssValue("opacity")
        let display = element.GetCssValue("display")
        match (display, opacity) with
        | ("none", "1") -> true
        | _ -> false
      let elements = this.FindElements cssSelector
      match elements |> Array.filter(isShown) with
      | [||] -> ()
      | itemsDisplayed -> raise <| WebElementsAreDisplayed itemsDisplayed
