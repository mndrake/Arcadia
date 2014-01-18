namespace Utopia

open System
open System.Collections.Generic
open System.Linq
open System.Threading


/// base class for Calculation Nodes
[<AbstractClass>]
type NodeBase<'U>(calculationHandler : ICalculationHandler, id, initialValue) = 
    
    member val internal value = match initialValue with
                                | Some v -> ref v
                                | None -> ref Unchecked.defaultof<'U>

    member val internal cts = 
        CancellationTokenSource.CreateLinkedTokenSource(
            calculationHandler.CancellationToken, new CancellationToken()) with get,set

    member this.AsyncCalculate() = 
        if calculationHandler.CancellationToken.IsCancellationRequested then
            calculationHandler.Reset()
        this.cts <- CancellationTokenSource.CreateLinkedTokenSource(
                        calculationHandler.CancellationToken, new CancellationToken())
        // check that dependent nodes are not dirty
        Async.Start((async { do! this.Evaluate() |> Async.Ignore }), this.cts.Token)

    abstract GetDependentNodes : unit -> INode []
    abstract Evaluate : unit -> Async<NodeStatus * obj>
    abstract IsInput : bool with get
    abstract Value : 'U with get, set
    abstract IsDirty : bool with get
    abstract IsProcessing : bool with get
    abstract Status : NodeStatus with get
    [<CLIEvent>]
    abstract Cancelled : IEvent<CancelledEventHandler, EventArgs>
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, EventArgs>

    member this.ToINode() = this :> INode<'U>
    member this.Calculation = calculationHandler
    member this.Id = id
    abstract RaiseChanged : unit -> unit

    interface INode<'U> with
        member this.AsyncCalculate() = this.AsyncCalculate()
        member this.Calculation = this.Calculation
        member this.Status = this.Status
        [<CLIEvent>]
        member this.Changed = this.Changed
        [<CLIEvent>]
        member this.Cancelled = this.Cancelled
        member this.GetDependentNodes() = this.GetDependentNodes()
        member this.IsDirty = this.IsDirty
        member this.Evaluate() = this.Evaluate()
        member this.Id = this.Id
        member this.IsInput = this.IsInput
        member this.IsProcessing = this.IsProcessing
        member this.RaiseChanged() = this.RaiseChanged()
        member this.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v      
        member this.Value 
            with get () = this.Value
            and set v = this.Value <- v
