namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Arcadia.Cell")>]
[<assembly: ComVisibleAttribute(false)>]
[<assembly: AssemblyProductAttribute("Arcadia.Cell")>]
[<assembly: AssemblyDescriptionAttribute("An asynchronous calculation framework for MVVM Models")>]
[<assembly: AssemblyVersionAttribute("0.1.0")>]
[<assembly: AssemblyFileVersionAttribute("0.1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.0"

namespace Microsoft.FSharp

//[<assembly: System.Security.SecurityTransparent>]
[<assembly: AutoOpen("Microsoft.FSharp")>]
[<assembly: AutoOpen("Microsoft.FSharp")>]
do()

