﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Arcadia.MVVM")>]
[<assembly: AssemblyProductAttribute("Arcadia.MVVM")>]
[<assembly: AssemblyDescriptionAttribute("Lightweight MVVM Utility Library")>]
[<assembly: AssemblyVersionAttribute("1.0.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.0"