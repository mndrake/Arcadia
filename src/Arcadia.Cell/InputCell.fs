namespace Arcadia.Cells

open System
open System.ComponentModel
open Helpers

/// input cell used within a CalculationEngine
type InputCell<'U>(id, ?initialValue) as this = 
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()

    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()

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
                        r.Reply(CellStatus.Valid, box !value)
                        return! valid()
                    | _ -> return! valid()
                }
            valid())
    
    do 
        propertyChanged <- castAs<INotifyPropertyChanged>(!value)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun _ -> 
                changed.Trigger(this, ChangedEventArgs(CellStatus.Valid))
                this.RaisePropertyChanged "Value")

        (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))

    
    new(?initialValue) = 
        match initialValue with
        | Some v -> InputCell("", v)
        | None -> InputCell("")

    abstract OnPropertyChanged : string -> unit
    override this.OnPropertyChanged(_) = ()

    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])


    member this.Id = id
    
    member this.Value 
        with get () = !value
        and set v = 
            value := v
            changed.Trigger(this, ChangedEventArgs(CellStatus.Valid))
    
    [<CLIEvent>]
    member this.Changed = changed.Publish

    member this.ToICell() = this :> ICell<'U>

    interface ICell<'U> with
        member this.Status = CellStatus.Valid
        
        [<CLIEvent>]
        member this.Changed = changed.Publish
        
        member this.GetDependentCells() = [||]
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

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish