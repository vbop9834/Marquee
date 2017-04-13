// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Web site location for the generated documentation
#if TESTING
let website = __SOURCE_DIRECTORY__ + "../output"
#else
let website = "/Marquee"
#endif

let githubLink = "http://github.com/jeremybellows/marquee"

// Specify more information about your project
let info =
  [ "project-name", "Marquee"
    "project-author", "Jeremy Bellows"
    "project-summary", "A Selenium based, canopy inspired, UI Automation library"
    "project-github", githubLink
    "project-nuget", "http://nuget.org/packages/JeremyBellows" ]

let referenceBinaries =
  [ "FSharp.CodeFormat.dll"; "FSharp.Literate.dll"; "FSharp.Markdown.dll"; "FSharp.MetadataFormat.dll"; "FSharp.Formatting.Common.dll" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------


#load "../packages/FSharp.Formatting/FSharp.Formatting.fsx"
#I "../packages/FAKE/tools/"
#r "FakeLib.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "./../build"
let content    = __SOURCE_DIRECTORY__ @@ "./content"
let output     = __SOURCE_DIRECTORY__ @@ "./output"
let files      = __SOURCE_DIRECTORY__ @@ "./files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../packages/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRoots =
  [ templates; formatting @@ "templates"
    formatting @@ "templates/reference"
  ]

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output @@ "content")

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  for lib in referenceBinaries do
    MetadataFormat.Generate
      ( bin @@ lib, output, layoutRoots,
        parameters = ("root", root)::info,
        sourceRepo = githubLink @@ "tree/scaffold", // TODO: revert to "tree/master"
        sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
        publicOnly = true,
        libDirs = [__SOURCE_DIRECTORY__ @@ ".." @@ "build"])

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots )

// Generate
copyFiles()
buildDocumentation()
buildReference()
