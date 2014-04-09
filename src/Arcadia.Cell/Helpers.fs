namespace Arcadia.Cells

open System.ComponentModel

/// internal utility functions
module Helpers = 
    /// checks if value is a F# Tuple type
    let isTuple value = 
        match box value with
        | null -> false
        | _ -> Microsoft.FSharp.Reflection.FSharpType.IsTuple(value.GetType())

    // http://stackoverflow.com/questions/2361851/c-sharp-and-f-casting-specifically-the-as-keyword
    /// F# implementation of the C# 'as' keyword
    let castAs<'T when 'T : null>(o : obj) = 
        match o with
        | :? 'T as res -> res
        | _ -> null

    /// checks to see if object is of type 'T
    let isType<'T> o =
        match box o with 
        | :? 'T -> true
        | _ -> false
