namespace Arcadia

open System
open System.ComponentModel
open Helpers

[<Sealed>]
/// input node used within a CalculationEngine
type Setable<'U>(calculationHandler : ICalculationHandler, ?initialValue : 'U) as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    let mutable equalityComparer = fun (a:'U) (b:'U) -> obj.ReferenceEquals(a, b) || (not <| obj.ReferenceEquals(a, null) && obj.Equals(a, b))
    let mutable id = ""
    let status = ref NodeStatus.Uninitialized
    let value = 
        match initialValue with
        | Some v -> 
            status := NodeStatus.Valid
            ref v
        | None -> ref Unchecked.defaultof<'U>
    
    let mutable propertyChanged : INotifyPropertyChanged = null
    
    let agent = 
        MailboxProcessor.Start(fun inbox -> 
            let rec valid() = 
                async { 
                    let! msg = inbox.Receive()
                    match msg with
                    | Eval r -> 
                        r.Reply(NodeStatus.Valid, box !value)
                        return! valid()
                    | _ -> return! valid()
                }
            valid())
    
    let mutable evaluate = fun () -> agent.PostAndAsyncReply(fun r -> Eval r)
    do 
        propertyChanged <- castAs<INotifyPropertyChanged>(!value)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun _ -> 
                changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
                this.RaisePropertyChanged "Value")

        (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))

    new (?initialValue) = 
        match initialValue with
        |Some v -> Setable(new CalculationHandler(Automatic = true), v)
        |None -> Setable(new CalculationHandler(Automatic = true), Unchecked.defaultof<'U>)

    abstract OnPropertyChanged : string -> unit
    override this.OnPropertyChanged(_) = ()

    member this.Status 
        with get () = !status
         and internal set v = status := v

    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])

    member this.Id
        with get() = id
         and set v = id <- v

    member internal this.SetValue v = value := v

    member this.Value 
        with get () =
            ComputedStack.Listeners.Notify(this)        
            !value
        and set v = 
            if not <| this.EqualityComparer !value v then
                value := v
                if !status = NodeStatus.Uninitialized then status := NodeStatus.Valid
                changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
    
    member this.Calculation = calculationHandler

    [<CLIEvent>]
    member this.Changed = changed.Publish

    member internal this.OnChanged(status) = 
        changed.Trigger(this, ChangedEventArgs(status))

    member this.EqualityComparer 
        with get() = equalityComparer
         and set v = equalityComparer <- v

    static member DefaultEqualityComparer(a:'U) (b:'U) =
        obj.ReferenceEquals(a, b) || (not <| obj.ReferenceEquals(a, null) && obj.Equals(a, b))

    member internal this.EvaluateSet e = evaluate <- e

    member this.Evaluate() = evaluate()

    interface IEquate<'U> with    
        member this.EqualityComparer 
            with get () = this.EqualityComparer
            and set v = this.EqualityComparer <- v

    interface ISetable<'U> with
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.Evaluate() = this.Evaluate()
        member this.OnChanged(status) = changed.Trigger(this, ChangedEventArgs(status))
        member this.Value 
            with get() = this.Value
             and set v = this.Value <- v
        member this.Id = this.Id

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish

    static member op_Implicit (from : Setable<'U>) = from.Value

    member this.ToINode() = this :> INode<'U>

    interface INode<'U> with
        member this.AsyncCalculate() = ()
        member this.Calculation = calculationHandler
        member this.Status = NodeStatus.Valid
        [<CLIEvent>]
        member this.Changed = changed.Publish
        
        member this.GetDependentNodes() = [||]
        member this.IsDirty = false
        member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
        member this.Id = this.Id
        member this.IsInput = true
        member this.IsProcessing = false
        
        member this.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v
        
        member this.Value 
            with get () = this.Value
            and set v = this.Value <- v

[<RequireQualifiedAccess>]
module Setable =
    let From(initVal) = new Setable<'T>(initVal)
