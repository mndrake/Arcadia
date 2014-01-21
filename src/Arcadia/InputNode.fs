namespace Arcadia

open System
open System.ComponentModel
open Helpers

/// input node used within a CalculationEngine
type InputNode<'U>(calculationHandler, id, ?initialValue) as this = 
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let cancelled = new Event<CancelledEventHandler, EventArgs>()
    
    let value = 
        match initialValue with
        | Some v -> ref v
        | None -> ref Unchecked.defaultof<'U>
    
    let mutable propertyChanged = castAs<INotifyPropertyChanged>(!value)
    
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
    
    do 
        propertyChanged <- castAs<INotifyPropertyChanged>(!value)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun _ -> changed.Trigger(this, EventArgs.Empty))
    
    new(calculationHandler, ?initialValue) = 
        match initialValue with
        | Some v -> InputNode(calculationHandler, "", v)
        | None -> InputNode(calculationHandler, "")

    member this.Id = id
    
    member this.Value 
        with get () = !value
        and set v = 
            value := v
            changed.Trigger(this, EventArgs.Empty)
    
    member this.Calculation = calculationHandler
    
    [<CLIEvent>]
    member this.Changed = changed.Publish

    member this.ToINode() = this :> INode<'U>

    interface INode<'U> with
        member this.AsyncCalculate() = ()
        member this.Calculation = calculationHandler
        member this.Status = NodeStatus.Valid
        
        [<CLIEvent>]
        member this.Changed = changed.Publish
        
        [<CLIEvent>]
        member this.Cancelled = cancelled.Publish
        
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
