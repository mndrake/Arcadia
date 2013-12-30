namespace Utopia

open System

/// base class for Calculation Nodes
[<AbstractClass>]
type NodeBase<'U>(calculationHandler : ICalculationHandler, id) as this = 
    
    [<CLIEvent>] abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    
    abstract DependentNodes : INode [] with get
    abstract Dirty : bool with get
    abstract Eval : Async<obj> with get
    abstract IsInput : bool with get
    abstract Processing : bool with get
    abstract Update : unit -> unit
    abstract Value : 'U with get, set
    member this.Calculation = calculationHandler
    member this.ID = id
    interface INode<'U> with
        member I.Calculation = this.Calculation
        
        [<CLIEvent>]
        member I.Changed = this.Changed
        
        member I.DependentNodes = this.DependentNodes
        member I.Dirty = this.Dirty
        member I.Eval = this.Eval
        member I.ID = this.ID
        member I.IsInput = this.IsInput
        member I.Processing = this.Processing
        member I.Update() = this.Update()
        member I.UntypedValue
            with get() = box this.Value
             and set v = this.Value <- unbox v
        member I.Value
            with get() = this.Value
             and set v = this.Value <- v
