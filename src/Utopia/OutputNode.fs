namespace Utopia

open Helpers
open Microsoft.FSharp.Reflection
open System
open System.ComponentModel
open System.Diagnostics
open System.Linq
open System.Threading

type OutputNode<'N, 'T, 'U>(calculationHandler, id, nodeInputs : 'N, nodeFunction : 'T -> 'U, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)
    
    // convert input nodes and function into array versions to ease parallel async support
    let nodes = 
        if isTuple nodeInputs then FSharpValue.GetTupleFields(nodeInputs) |> Array.map(fun x -> x :?> INode)
        else [| (box nodeInputs) :?> INode |]
    
    let func = 
        if isTuple nodeInputs then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> nodeFunction
        else fun p -> (p.[0] :?> 'T) |> nodeFunction
    
    // debug messages
    let msgCancel() = Debug.Print("EVAL CANCELLED : ID {0}, Dirty {1}", this.ID, this.Dirty)
    let msgUpdate() = Debug.Print("ID {0} VALUE UPDATED : THREAD {1}", this.ID, Thread.CurrentThread.ManagedThreadId)
    
    do 
        this.dirty := true
        nodes |> Seq.iter(fun n -> 
                     n.Changed.Add(fun args -> 
                         this.dirty := true
                         this.RaiseChanged()))
        this.Changed.Add
            (fun args -> 
            if not !this.processing && this.Calculation.Automatic && nodes.All(fun n -> not n.Dirty) then 
                async { do! this.Eval |> Async.Ignore } |> Async.Start)
    
    override this.DependentNodes = nodes
    
    override this.Eval = 
        async { 
            use! cancelHandler = Async.OnCancel(fun () -> 
                                     this.processing := false
                                     msgCancel())
            if !this.dirty && not !this.processing then 
                this.processing := true
                let! values = nodes
                              |> Seq.map(fun n -> n.Eval)
                              |> Async.Parallel
                let! result = async { return func values }
                this.initValue := result
                msgUpdate()
                this.dirty := false
                this.processing := false
                this.RaiseChanged()
            while (!this.processing) do
                do! Async.Sleep(100)
            return box <| !this.initValue
        }
    
    override this.IsInput = false
    
    override this.Value 
        with get () = !this.initValue
        and set v = invalidArg "Value" "cannot set value of an output node"
