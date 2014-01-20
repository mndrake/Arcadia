// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open Fake 
open Fake.AssemblyInfoFile

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "Arcadia"
let authors = ["David Carlson"]
let summary = "An asynchronous calculation framework for MVVM Models"
let description = """
  An asynchronous calculation framework for MVVM Models"""
let tags = "F# MVVM"

let gitHome = "https://github.com/mndrake"
let gitName = "Arcadia"

RestorePackages()

// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = 
    File.ReadLines "RELEASE_NOTES.md" 
    |> ReleaseNotesHelper.parseReleaseNotes

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target "AssemblyInfo" (fun _ ->
    [ ("src/Arcadia/AssemblyInfo.fs", "Arcadia", project, summary) ]
    |> Seq.iter (fun (fileName, title, project, summary) ->
        CreateFSharpAssemblyInfo fileName
           [ Attribute.Title title
             Attribute.ComVisible false
             Attribute.Product project
             Attribute.Description summary
             Attribute.Version release.AssemblyVersion
             Attribute.FileVersion release.AssemblyVersion ] )
)

// --------------------------------------------------------------------------------------
// directory definitions

let buildDir = "bin"
let testDir = "test"

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ -> CleanDirs [ buildDir; testDir; "deploy"; "deploy/FSharpApp"; "deploy/CSharpApp"; "deploy/Arcadia" ])

// --------------------------------------------------------------------------------------
// Build library (builds Visual Studio solution)

Target "Build" (fun _ ->
    MSBuildRelease buildDir "Rebuild" ["Arcadia.sln"]
    |> Log "Build-Output: "
)

// --------------------------------------------------------------------------------------
// Setup Deployment Folders

Target "Deploy" (fun _ ->
    
    let common = 
        !! "bin/App.Model.dll"
        ++ "bin/FSharp.Core.dll"
        ++ "bin/GraphSharp.Controls.dll"
        ++ "bin/GraphSharp.dll"
        ++ "bin/MahApps.Metro.dll"
        ++ "bin/QuickGraph.dll"
        ++ "bin/System.Windows.Interactivity.dll"
        ++ "bin/Arcadia.dll"
        ++ "bin/WPFExtensions.dll"

    // setup FSharpApp sample
    common
    ++ "bin/FSharp*.*"
    |> CopyFiles "deploy/FSharpApp/"

    // setup CSharpApp sample
    common
    ++ "bin/CSharpApp*.*"
    |> CopyFiles "deploy/CSharpApp/"
        
    // setup Utopia deployment
    !! "bin/Arcadia.*"
    |> CopyFiles "deploy/Arcadia"

    )

// --------------------------------------------------------------------------------------
// FxCop - Code Analysis 
// /c /f:$(TargetPath) /d:$(BinDir) /r:"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Team Tools\Static Analysis Tools\FxCop\Rules"
Target "FxCop" (fun () ->  
//    !! (buildDir + @"\**\*.dll") 
//    ++ (buildDir + @"\**\*.exe") 
    !! ("bin/Arcadia.dll")
    |> FxCop 
        (fun p -> 
            {p with 
              // override default parameters
              ReportFileName = buildDir + "/FXCopResults.xml"
              ToolPath = """C:\Program Files (x86)\Microsoft Visual Studio 11.0\Team Tools\Static Analysis Tools\FxCop\FxCopCmd.exe"""})
)


// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
            Version = release.NugetVersion
            ReleaseNotes = release.Notes |> String.concat "\n"
            Tags = tags
            OutputPath = "bin"
            ToolPath = ".nuget/nuget.exe"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/Arcadia.nuspec"
)

// --------------------------------------------------------------------------------------
// Help

Target "Help" (fun _ ->
    printfn ""
    printfn "  Please specify the target by calling 'build <Target>'"
    printfn ""
    printfn "  Targets for building:"
    printfn "  * Build"
    printfn ""
    printfn "  Targets for releasing:"
    printfn "  * NuGet (creates package only, doesn't publish)"
    printfn "")

Target "All" DoNothing

"Clean" 
==> "AssemblyInfo" 
==> "Build" 
==> "NuGet" 
==> "All"

"Build"
==> "Deploy"

"Build"
==> "FxCop"

RunTargetOrDefault "Help"