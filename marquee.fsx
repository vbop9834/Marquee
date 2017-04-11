#I __SOURCE_DIRECTORY__
#I "./packages/Selenium.WebDriver/lib/net40"
#I "./marquee"

#r "WebDriver.dll"
#load "marquee.fs"

open marquee
Firefox __SOURCE_DIRECTORY__ |> Browser.Create
