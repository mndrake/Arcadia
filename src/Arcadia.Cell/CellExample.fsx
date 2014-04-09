#r "bin/Debug/Arcadia.Cell.dll"
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations
//open Microsoft.FSharp.Linq.QuotationEvaluation
open Arcadia.Cells

open Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter
//open Arcadia.Cells.Expressions

type QuoteBuilder<'U>() =
    member this.Quote p = p
    member this.Return p = p

let quote<'U> = QuoteBuilder<'U>()
   
module Cell =
    let value v = InputCell v
    let compute (expr : Expr<'U>) = 
        let eval e = EvaluateQuotation(e) |> unbox
        let cellFunc : unit -> 'U = fun() -> eval expr
        let cellDep : ICell list = 
            let rec loop e acc = 
                match e with
                | Call(_, _, p) -> p |> List.collect (fun q -> loop q [])
                | Let(_, p1, p2) -> [p1;p2] |> List.collect (fun q -> loop q [])
                | Lambda(_, p) -> loop p []
                | Coerce(p, t) -> loop p []
                | NewUnionCase(_, p) -> p |> List.collect (fun q -> loop q [])
                | PropertyGet(None, i, _) -> 
                    (Expr.Cast<ICell>(Expr.Coerce(e, typeof<ICell>)) |> eval) :: acc
                | PropertyGet(Some p, i, _) -> loop p acc
                | Value p -> []
                | p -> []
            loop expr []
        OutputCell<'U>(cellDep |> Seq.distinct, cellFunc)

let add3 (x1,x2,x3) = x1+x2+x3

let in1 = Cell.value 5
let in2 = Cell.value 2
let out2 = Cell.compute(<@ in1.Value + in2.Value + in1.Value @>)
let out3 = Cell.compute(<@ add3(in1.Value, in2.Value, in1.Value) @>)


//#region Builder
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


//#endregion

type Person = {Name:string; Age:int}

let addThem(x1,x2) =
    let res = x1+x2
    printfn "res: %i" res
    res

let p1 = InputCell {Name = "Bob"; Age = 32}
let p2 = InputCell {Name = "Tom"; Age = 26}

let maxAge = cell {
    let! a = [p1;p2] |> List.maxBy (fun p -> p.Value.Age)
    return a.Age}

let avgAge = cell {
    let a = [p1;p2] |> List.averageBy (fun p -> float p.Value.Age)
    return a}

maxAge.Value
avgAge.Value
avgAge.GetDependentCells()

p2.Value <- {Name = "Chuck"; Age = 50}
p2.Value <- {Name = "Tom"; Age = 26}

let in1 = cell { return 4 }
let in2 = cell { return 6 }

let out1 = cell { return in1.Value + in2.Value }

let out2 = cell {
    let! x = in1
    let! y = in2
    return x + y }
        
out2.Value