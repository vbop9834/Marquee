#I __SOURCE_DIRECTORY__
#I "./packages/Selenium.WebDriver/lib/net40"
#I "./marquee"

#r "WebDriver.dll"
#load "marquee.fs"

open marquee
let browser = Chrome __SOURCE_DIRECTORY__ |> Browser.Create
browser.Url "http://lefthandedgoat.github.io/canopy/testpages/"
"#button_clicked" |> browser.ElementTextEquals "button not clicked"
browser.Click "#button"
"#button_clicked" |> browser.ElementTextEquals "button clicked"
browser.Displayed "#welcome"
"#welcome" |> browser.ElementTextEquals "Welcome"
