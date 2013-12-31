namespace Utopia

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

[<RequireQualifiedAccess>]
module Array2D = 
    let cast<'a>(A : obj [,]) : 'a [,] = A |> Array2D.map unbox
    let flatten<'a>(A : 'a [,]) = A |> Seq.cast<'a>
    let getColumn c (A : _ [,]) = flatten A.[*, c..c] |> Seq.toArray
    let getColumns(A : _ [,]) = [| for i = 0 to A.GetLength(1) - 1 do yield A |> getColumn i |]  
    let getRow<'T> r (A : 'T [,]) = flatten<'T> A.[r..r, *] |> Seq.toArray
    let getRows<'T>(A : 'T [,]) = [| for i = 0 to A.GetLength(0) - 1 do yield A |> getRow<'T> i |]
    let toArray<'a>(A : obj [,]) = A |> Seq.cast<'a> |> Seq.toArray
    let ofColumnArray(A : 'a []) = Array2D.init (A.Length) 1 (fun i j -> A.[i])
