namespace Arcadia.Cells

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Linq.QuotationEvaluation

module Expressions =

    type CellBuilder<'U>() = 
        member this.Quote x = x
    
        member this.Run expr = 
            let depNodes : ICell list = 
                let rec loop e acc = 
                    match e with
                    | Call(_, _, p) -> p |> List.collect (fun q -> loop q [])
                    | Let(_, p1, p2) -> [p1;p2] |> List.collect (fun q -> loop q [])
                    | Lambda(_, p) -> loop p []
                    | Coerce(p, t) -> loop p []
                    | NewUnionCase(_, p) -> p |> List.collect (fun q -> loop q [])
                    | PropertyGet(None, i, _) -> 
                        (Expr.Coerce(e, typeof<ICell>) |> Expr.Cast).Eval() :: acc
                    | PropertyGet(Some p, i, _) -> loop p acc
                    | Value p -> []
                    | p -> []
                loop expr []
        
            let func : unit -> 'U = (expr |> Expr.Cast).Eval()
            if depNodes.Length = 0 then InputCell(func()) :> ICell<'U>
            else OutputCell(depNodes, func) :> ICell<'U>
        member this.Yield p = p
        member this.Return(x : 'U) : 'U = x
        member this.Bind(p : ICell<'T>, rest : 'T -> 'U) : 'U = p.Value |> rest
        member this.Let(p, rest) = rest p
        member this.Delay(f : unit -> 'U) : unit -> 'U = f
        member this.For(expr, rest) =
            expr |> Seq.map (fun p -> rest p)

    let cell<'U> = new CellBuilder<'U>()