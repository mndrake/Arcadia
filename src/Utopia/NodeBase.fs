namespace Utopia

open System
open System.Collections.Generic
open System.Linq
open System.Threading


/// base class for Calculation Nodes
[<AbstractClass>]
type NodeBase<'U>(calculationHandler : ICalculationHandler, id, initialValue) as this = 
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let mutable cts : CancellationTokenSource option = None
    member val internal dirty = ref true
    member val internal processing = ref false
    
    member val internal initValue = match initialValue with
                                    | Some v -> ref v
                                    | None -> ref Unchecked.defaultof<'U>
    
    abstract GetDependentNodes : unit -> INode []
    abstract Computation : Async<obj> with get
    abstract IsInput : bool with get
    abstract Status : NodeStatus
    abstract Value : 'U with get, set
    member this.ToINode() = this :> INode<'U>
    member this.Cancel() = 
        match cts with
        |Some c -> c.Cancel()
        |None -> ()
    member this.AsyncCalculate() = 
        if calculationHandler.CancellationToken.IsCancellationRequested then
            calculationHandler.Reset()
        let source = new CancellationTokenSource()
        cts <- Some <| CancellationTokenSource.CreateLinkedTokenSource(calculationHandler.CancellationToken, source.Token)
        // check that dependent nodes are not dirty
        Async.Start((async { do! this.Computation |> Async.Ignore }), cts.Value.Token)
   
    member this.Calculation = calculationHandler
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    member this.Dirty = !this.dirty
    member this.Id = id
    member this.Processing = !this.processing
    member this.RaiseChanged() = changed.Trigger(this, EventArgs.Empty)
    interface INode<'U> with
        member I.AsyncCalculate() = this.AsyncCalculate()
        member I.Calculation = this.Calculation
        
        [<CLIEvent>]
        member I.Changed = this.Changed
        
        member I.GetDependentNodes() = this.GetDependentNodes()
        member I.Dirty = this.Dirty
        member I.Computation = this.Computation
        member I.Id = this.Id
        member I.IsInput = this.IsInput
        member I.Processing = this.Processing
        member I.RaiseChanged() = this.RaiseChanged()
        member I.Status = this.Status
        member I.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v
        
        member I.Value 
            with get () = this.Value
            and set v = this.Value <- v
