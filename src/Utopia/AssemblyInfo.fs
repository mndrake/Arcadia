namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Utopia")>]
[<assembly: AssemblyProductAttribute("Utopia")>]
[<assembly: AssemblyDescriptionAttribute("An asynchronous node based calculation framework for F# Models (MVVM)")>]
[<assembly: AssemblyVersionAttribute("0.0.0")>]
[<assembly: AssemblyFileVersionAttribute("0.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.0"
