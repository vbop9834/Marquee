#I __SOURCE_DIRECTORY__
#I "./packages/Selenium.WebDriver/lib/net40"
#I "./marquee"

#r "WebDriver.dll"
#load "marquee.fs"
#load "testManager.fs"

open marquee
open testManager

let testManager = Chrome __SOURCE_DIRECTORY__ |> TestManager.Create
let (--) testDescription testFunc = testManager.Register (testDescription, testFunc)

"Button should click" -- fun browser ->
  browser.Url "http://lefthandedgoat.github.io/canopy/testpages/"
  "#button_clicked" |> browser.ElementTextEquals "button not clicked"
  browser.Click "#button"
  "#button_clicked" |> browser.ElementTextEquals "button clicked"
  browser.Displayed "#welcome"
  "#welcome" |> browser.ElementTextEquals "Welcome1"

testManager.RunTests ()
