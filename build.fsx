// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open Fake 
open Fake.AssemblyInfoFile
open Fake.Git

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

let releaseNotes = release.Notes |> String.concat "\n"

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
// Clean build results

Target "Clean" (fun _ -> CleanDirs ["bin";"temp"])

Target "CleanDocs" (fun _ -> CleanDirs ["docs/output"])


// --------------------------------------------------------------------------------------
// Build library (builds Visual Studio solution)

Target "Build" (fun _ ->
    !! "Arcadia.sln"
    |> MSBuildRelease "" "Rebuild"
    |> Log "Build-Output: "
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
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/gh-pages"
    Branches.checkoutBranch "temp/gh-pages" "gh-pages"
    CopyRecursive "docs/output" "temp/gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
    Branches.push "temp/gh-pages"
)

Target "ReleaseBinaries" (fun _ ->
    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/release"
    Branches.checkoutBranch "temp/release" "release"
    CopyRecursive "bin" "temp/release" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "temp/release" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update binaries for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand "temp/release" cmd |> printfn "%s"
    Branches.push "temp/release"
)
//
//
//Target "ReleaseDocs" (fun _ ->
//    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/gh-pages"
//    Branches.checkoutBranch "temp/gh-pages" "gh-pages"
//    CopyRecursive "docs/output" "temp/gh-pages" true |> printfn "%A"
//    CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
//    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
//    CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
//    Branches.push "temp/gh-pages"
//)
//
//Target "ReleaseBinaries" (fun _ ->
//    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/release"
//    Branches.checkoutBranch "temp/release" "release"
//    CopyRecursive "bin" "temp/release/bin" true |> printfn "%A"
//    let cmd = sprintf """commit -a -m "Update binaries for version %s""" release.NugetVersion
//    CommandHelper.runSimpleGitCommand "temp/release" cmd |> printfn "%s"
//    Branches.push "temp/release"
//)

Target "Release" DoNothing

"CleanDocs" ==> "GenerateDocs" ==> "ReleaseDocs"
"ReleaseDocs" ==> "Release"
"ReleaseBinaries" ==> "Release"
"NuGet" ==> "Release"

// --------------------------------------------------------------------------------------
// Help

Target "Help" (fun _ ->
    printfn ""
    printfn "  Please specify the target by calling 'build <Target>'"
    printfn ""
    printfn "  Targets for building:"
    printfn "  * Build"
    printfn "  * All (calls previous 1)"
    printfn ""
    printfn "  Targets for releasing (requires write access to the 'https://github.com/mndrake/Arcadia.git' repository):"
    printfn "  * GenerateDocs"
    printfn "  * ReleaseDocs (calls previous)"
    printfn "  * ReleaseBinaries"
    printfn "  * NuGet (creates package only, doesn't publish)"
    printfn "  * Release (calls previous 4)"
    printfn "")

Target "All" DoNothing

"Clean" ==> "AssemblyInfo" ==> "Build"
"Build" ==> "All"


RunTargetOrDefault "Help"