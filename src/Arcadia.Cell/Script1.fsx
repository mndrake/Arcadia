#r "bin/Debug/Arcadia.Cell.dll"
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

type CellValueType =
| CellValue of int
| CellFunction of (unit -> int)

type Cell(v:CellValueType) =
    let mutable value = v
    member this.Value = 
        match value with
        |CellValue v -> v
        |CellFunction f -> f()
    member this.SetValue v =
        value <- CellValue v
    member this.SetFunction f =
        value <- CellFunction f
    static member (+) (a:Cell, b:Cell) = a.Value + b.Value
    static member (-) (a:Cell, b:Cell) = a.Value - b.Value
    static member (*) (a:Cell, b:Cell) = a.Value * b.Value
    static member (/) (a:Cell, b:Cell) = a.Value / b.Value
    static member (~-) (a:Cell) = -a.Value
    static member Map (f:int * int -> int) (a:Cell,b:Cell) = f (a.Value, b.Value)

type CellBuilder() =
    member this.Quote x = x
    member this.Run expr = 
        let depNodes : Cell list =
            let rec loop e acc =
                match e with
                | Call (_, _, p) -> p |> List.collect (fun q -> loop q [])
                | Let (_, _, p) -> loop p []
                | Lambda (_, p) -> loop p []
                | PropertyGet(None, i, _) -> (Expr.Coerce(e, typeof<Cell>) |> Expr.Cast).Eval() :: acc
                | PropertyGet(Some p, i, _) -> loop p acc
                | Value p -> []
                | _ -> []
            loop expr []
        let func:(unit -> int) = (expr |> Expr.Cast).Eval()
        printfn "expr: %A" expr
        Cell(CellFunction (func))
    member this.Return (x : int) : int = x
    member this.Bind(p : Cell, rest : int -> int) : int = p.Value |> rest
    member this.Let(p, rest) = rest p
    member this.Delay (f:unit -> int) : (unit -> int) = f

let cell = new CellBuilder()

let in1 = Cell(CellValue 4)
let in2 = Cell(CellValue 5)

let add2 (x1:int,x2:int):int = x1+x2

let t1 = 
    cell {let! a = in1
          return a}

let t2 = cell {return in1 + in2}
let t3 = cell { let! v1 = in1
                let! v2 = in2
                let v3 = t2.Value
                return v1 + v2 + v3}
let t4 = cell {return add2(in1.Value,in2.Value)}
t3
in1.SetValue 200
t2


let rec obs(initial) =
    fun() ->
        

//printfn "t1: %A" t1.Value
//in1.SetValue 400
//printfn "t1: %A" t1.Value


//let runScript (a:Script<'a>) = a()
//let delay f = fun () -> runScript (f ())


//type ScriptBuilder() =
//    /// this allows a value to be returned from a script
//    member b.Return(x) = fun() -> x
//    /// this allows the user to use a let value inside their scripts
//    member b.Bind(p,rest) = 
//        printfn "bind: %A" p
//        rest p
//    member b.Let(p,rest) =
//        printfn "let: %A" p
//        rest p
//    /// this delays the construction of a script until just before it is executed
//    member b.Delay(f) = delay f
//
//// the instance of the script builder is used in the workflows definition
//let script = new ScriptBuilder()
//
//let num = script { let x = 2
//                   let y = 21
//                   return x * y }
//
//runScript num

