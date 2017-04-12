#I __SOURCE_DIRECTORY__
#I "./packages/Selenium.WebDriver/lib/net40"
#I "./marquee"

#r "WebDriver.dll"
#load "marquee.fs"
#load "testManager.fs"

open marquee
open testManager

let amountOfBrowsers = 5
let testManager = Chrome __SOURCE_DIRECTORY__ |> TestManager.Create amountOfBrowsers
let (--) testDescription testFunc = testManager.Register (testDescription, testFunc)

let registerATest () =
  "Button should click" -- fun browser ->
    browser.Url "http://lefthandedgoat.github.io/canopy/testpages/"
    "#button_clicked" |> browser.ElementTextEquals "button not clicked"
    browser.Click "#button"
    "#button_clicked" |> browser.ElementTextEquals "button clicked"
    browser.Displayed "#welcome"
    "#welcome" |> browser.ElementTextEquals "Welcome"

registerATest ()
registerATest ()
registerATest ()
registerATest ()
registerATest ()
registerATest ()
registerATest ()
registerATest ()
registerATest ()

testManager.RunTests ()
testManager.EndManager ()
