namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Arcadia")>]
[<assembly: ComVisibleAttribute(false)>]
[<assembly: AssemblyProductAttribute("Arcadia")>]
[<assembly: AssemblyDescriptionAttribute("An asynchronous calculation framework for MVVM Models")>]
[<assembly: AssemblyVersionAttribute("0.0.5")>]
[<assembly: AssemblyFileVersionAttribute("0.0.5")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.5"
