namespace Utopia

open Helpers
open Microsoft.FSharp.Reflection
open System
open System.ComponentModel
open System.Diagnostics
open System.Threading

type OutputNode<'N, 'T, 'U>(calculationHandler, nodes : 'N, func : 'T -> 'U, id, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id)
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let dirty = ref true
    
    let initValue = 
        match initialValue with
        | Some v -> ref v
        | None -> ref Unchecked.defaultof<'U>
    
    let nodesArray = 
        if isTuple nodes then FSharpValue.GetTupleFields(nodes) |> Array.map(fun x -> x :?> INode)
        else [| (box nodes) :?> INode |]
    
    let processing = ref false
    
    do 
        nodesArray |> Seq.iter(fun n -> 
                          n.Changed.Add(fun args -> 
                              dirty := true
                              changed.Trigger(this, EventArgs.Empty)))
        this.Changed.Add
            (fun args -> 
            if (not !processing) && (this.Calculation.Automatic) && (nodesArray |> Seq.forall(fun n -> not n.Dirty)) then 
                async { do! this.Eval |> Async.Ignore } |> Async.Start)
    
    override this.Processing = !processing
    
    [<CLIEvent>]
    override this.Changed = changed.Publish
    
    override this.DependentNodes = nodesArray
    override this.Dirty = !dirty
    
    override this.Eval = 
        async { 
            use! cancelHandler = Async.OnCancel
                                     (fun () -> 
                                     processing := false
                                     Debug.Print
                                         (String.Format("-- canceling eval -- ID {0}, Dirty {1}", this.ID, this.Dirty)))
            if !dirty && not !processing then 
                processing := true
                let! arrayValues = nodesArray
                                   |> Seq.map(fun n -> n.Eval)
                                   |> Async.Parallel
                let arrayFunc = 
                    if isTuple nodes then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> func
                    else fun p -> (p.[0] :?> 'T) |> func
                let! result = async { return arrayFunc arrayValues }
                initValue := result
                Debug.Print
                    (String.Format
                         ("ID {0} VALUE {1} -- THREAD {2}", this.ID, !initValue, Thread.CurrentThread.ManagedThreadId))
                dirty := false
                processing := false
                changed.Trigger(this, EventArgs.Empty)
            while (!processing) do
                do! Async.Sleep(100)
            return box <| !initValue
        }
    
    override this.IsInput = false
    
    override this.Value 
        with get () = !initValue
        and set v = invalidArg "Value" "cannot set value of an output node"
    
    override this.Update() = async { do! this.Eval |> Async.Ignore } |> Async.Start
