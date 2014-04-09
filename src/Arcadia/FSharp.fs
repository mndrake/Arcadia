namespace Arcadia

// inline helper methods for FSharp
module FSharp =
    
//    let inline (!!) (arg:IGetable<'T>) = arg.Value
    let inline (!!) arg = (^T : (static member op_Implicit : ^T -> ^U) arg)
    let inline (<--) (arg:ISetable<'T>) value = arg.Value <- value