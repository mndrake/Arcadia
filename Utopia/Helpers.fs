module Helpers

    let isTuple value = 
        match box value with
        | null -> false
        | _ -> Microsoft.FSharp.Reflection.FSharpType.IsTuple(value.GetType())

    let castAs<'T when 'T : null> (o:obj) = 
        match o with
        | :? 'T as res -> res
        | _ -> null