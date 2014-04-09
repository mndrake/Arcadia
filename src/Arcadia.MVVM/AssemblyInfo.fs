namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Arcadia.MVVM")>]
[<assembly: ComVisibleAttribute(false)>]
[<assembly: AssemblyProductAttribute("Arcadia")>]
[<assembly: AssemblyDescriptionAttribute("An asynchronous calculation framework for MVVM Models")>]
[<assembly: AssemblyVersionAttribute("0.2.0")>]
[<assembly: AssemblyFileVersionAttribute("0.2.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.0"
