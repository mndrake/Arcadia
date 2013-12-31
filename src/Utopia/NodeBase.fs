namespace Utopia

open System

/// base class for Calculation Nodes
[<AbstractClass>]
type NodeBase<'U>(calculationHandler : ICalculationHandler, id, initialValue) as this = 
    let changed = new Event<ChangedEventHandler, EventArgs>()
    member val internal dirty = ref true
    member val internal processing = ref false
    
    member val internal initValue = match initialValue with
                                    | Some v -> ref v
                                    | None -> ref Unchecked.defaultof<'U>
    
    abstract DependentNodes : INode [] with get
    abstract Eval : Async<obj> with get
    abstract IsInput : bool with get
    abstract Value : 'U with get, set
    member this.AsyncCalculate() = async { do! this.Eval |> Async.Ignore } |> Async.Start
    member this.Calculation = calculationHandler
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    member this.Dirty = !this.dirty
    member this.ID = id
    member this.Processing = !this.processing
    member this.RaiseChanged() = changed.Trigger(this, EventArgs.Empty)
    interface INode<'U> with
        member I.AsyncCalculate() = this.AsyncCalculate()
        member I.Calculation = this.Calculation
        
        [<CLIEvent>]
        member I.Changed = this.Changed
        
        member I.DependentNodes = this.DependentNodes
        member I.Dirty = this.Dirty
        member I.Eval = this.Eval
        member I.ID = this.ID
        member I.IsInput = this.IsInput
        member I.Processing = this.Processing
        member I.RaiseChanged() = this.RaiseChanged()
        
        member I.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v
        
        member I.Value 
            with get () = this.Value
            and set v = this.Value <- v
