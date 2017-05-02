// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git

// Directories
let buildDir  = sprintf "%s/build/" __SOURCE_DIRECTORY__
let deployDir = sprintf "%s/deploy/" __SOURCE_DIRECTORY__
let docsDir = sprintf "%s/docs" __SOURCE_DIRECTORY__
let docsOutputDir = sprintf "%s/output" docsDir

//Documentation
//Project Scaffold
let fakePath = "packages" @@ "FAKE" @@ "tools" @@ "FAKE.exe"
let fakeStartInfo script workingDirectory args fsiargs environmentVars =
    (fun (info: System.Diagnostics.ProcessStartInfo) ->
        info.FileName <- System.IO.Path.GetFullPath fakePath
        info.Arguments <- sprintf "%s --fsiargs -d:FAKE %s \"%s\"" args fsiargs script
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild" msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

let commandToolPath = "bin" @@ "fsformatting.exe"
let commandToolStartInfo workingDirectory environmentVars args =
    (fun (info: System.Diagnostics.ProcessStartInfo) ->
        info.FileName <- System.IO.Path.GetFullPath commandToolPath
        info.Arguments <- args
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild" msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Run the given buildscript with FAKE.exe
let executeWithOutput configStartInfo =
  let exitCode =
    ExecProcessWithLambdas
      configStartInfo
      System.TimeSpan.MaxValue false ignore ignore
  System.Threading.Thread.Sleep 1000
  exitCode
let executeHelper executer traceMsg failMessage configStartInfo =
    trace traceMsg
    let exit = executer configStartInfo
    if exit <> 0 then
        failwith failMessage
    ()

let execute = executeHelper executeWithOutput

let buildDocumentationTarget fsiargs target =
  execute
    (sprintf "Building documentation (%s), this could take some time, please wait..." target)
    "generating reference documentation failed"
      (fakeStartInfo "generate.fsx" "docs" "" fsiargs ["target", target])

// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "CleanDocs" (fun _ ->
    CleanDirs [docsOutputDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "Deploy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "ApplicationName." + version + ".zip")
)

Target "Test" (fun _ ->
               let result =
                 ExecProcess (fun info ->
                              info.FileName <- buildDir @@ "marquee.tests.exe"
                              info.WorkingDirectory <- buildDir
                              ) (System.TimeSpan.FromMinutes 5.0)
               if result <> 0 then failwith "Marquee tests failed"
)

Target "GenerateDocs" (fun _ ->
    buildDocumentationTarget "--define:RELEASE --define:REFERENCE --define:HELP" "Default")

// Build order
"Clean"
  ==> "Build"
  ==> "Test"
  ==> "Deploy"

"Clean"
  ==> "Build"
  ==> "CleanDocs"
  ==> "GenerateDocs"

// start build
RunTargetOrDefault "Test"
