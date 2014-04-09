namespace Arcadia

open System
open System.Collections
open System.Collections.Generic
open System.ComponentModel
open Helpers

[<Sealed>]
type SetableList<'U>(calculationHandler : ICalculationHandler, ?initialValue : IEnumerable<'U>) as this =
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    let mutable equalityComparer = Setable<'U>.DefaultEqualityComparer
    let value = 
        let list = List<'U>()
        match initialValue with
        | Some l -> list.AddRange(l)
        | None -> ()
        ref list
    let _updatingIndexes = new Stack<int>()
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    let added = Event<ListEventHandler, ListEventArgs>()
    let updated = new Event<ListEventHandler, ListEventArgs>()
    let removed = new Event<ListEventHandler, ListEventArgs>()
    let cleared = new Event<EventHandler, EventArgs>()
    let mutable id = ""
    let status = ref NodeStatus.Valid
    
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

    let evaluate = fun () -> agent.PostAndAsyncReply(fun r -> Eval r)
    do 
        propertyChanged <- castAs<INotifyPropertyChanged>(!value)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun _ -> 
                changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
                this.RaisePropertyChanged "Value")

        (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))
//        added.Publish.Add(fun _ -> changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid)))
//        removed.Publish.Add(fun _ -> changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid)))
//        cleared.Publish.Add(fun _ -> changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid)))
//        updated.Publish.Add(fun _ -> changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid)))

    new (?initialValue) = 
        match initialValue with
        |Some v -> SetableList(new CalculationHandler(Automatic = true), v)
        |None -> SetableList(new CalculationHandler(Automatic = true), new List<_>())

    member this.EqualityComparer 
        with get() = equalityComparer
         and set v = equalityComparer <- v
    
    member this.Value 
        with get() =
            ComputedStack.Listeners.Notify(this)
            !value
         and set _ = failwith "cannot directly set the list of a settable list"

    member this.Evaluate() = evaluate()
 
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
    
    [<CLIEvent>] member this.Changed = changed.Publish
    [<CLIEvent>] member this.Added = added.Publish
    [<CLIEvent>] member this.Updated = updated.Publish
    [<CLIEvent>] member this.Removed = removed.Publish
    [<CLIEvent>] member this.Cleared = cleared.Publish
    
    member this.Add(item) = 
        (!value).Add(item)
        this.Writing((fun i -> added.Trigger(this, ListEventArgs(i))), (!value).Count - 1)
    
    member this.IndexOf(item) = 
        ComputedStack.Listeners.Notify(this)
        (!value).IndexOf(item)

    member this.Calculation = calculationHandler
    
    member this.Clear() = 
        (!value).Clear()
        this.Writing((fun _ -> cleared.Trigger(this, EventArgs.Empty)), -1)
        cleared.Trigger(this, EventArgs.Empty)
    
    member private this.Writing(also : int -> unit, index : int) = 
        changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
        also index
    
    member this.Insert(index, item) = 
        (!value).Insert(index, item)
        this.Writing((fun i -> added.Trigger(this, ListEventArgs(i))), index)
    
    member this.RemoveAt(index) = 
        (!value).RemoveAt(index)
        this.Writing((fun i -> removed.Trigger(this, ListEventArgs(i))), index)
    
    member this.Item 
        with get (index) = 
            ComputedStack.Listeners.Notify(this)
            (!value).[index]
        and set index v = 
            if not <| equalityComparer (!value).[index] v then 
                if _updatingIndexes.Contains(index) then 
                    failwith "circular dependency"
                (!value).[index] <- v
                _updatingIndexes.Push(index)
                try 
                    this.Writing((fun i -> updated.Trigger(this, ListEventArgs(i))), index)
                finally
                    _updatingIndexes.Pop() |> ignore
    
    member this.Contains(item) = 
        ComputedStack.Listeners.Notify(this)
        (!value).Contains(item)
    
    member this.CopyTo(array, arrayIndex) = 
        ComputedStack.Listeners.Notify(this)
        (!value).CopyTo(array, arrayIndex)
    
    member this.Count = 
        ComputedStack.Listeners.Notify(this)
        (!value).Count
    
    member this.IsReadOnly = false
    
    member this.Remove(item) = 
        ComputedStack.Listeners.Notify(this)
        let index = (!value).IndexOf(item)
        if index = -1 then false
        else 
            this.RemoveAt(index)
            true
    
    member this.GetEnumerator() = 
        ComputedStack.Listeners.Notify(this)
        (!value).GetEnumerator() :> IEnumerator<'U>

    [<CLIEvent>]
    member this.PropertyChanged = propertyChangedEvent.Publish
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = this.PropertyChanged

    static member op_Implicit (from : Setable<'U>) = from.Value

    interface IEnumerable with
        member this.GetEnumerator() = this.GetEnumerator() :> IEnumerator
    
    interface ISetableList<'U> with    
        member this.Item 
            with get index = this.[index]
            and set index value = this.[index] <- value        
        member this.IndexOf(item) = this.IndexOf(item)
        member this.Evaluate() = this.Evaluate()
        member this.OnChanged(status) = changed.Trigger(this, ChangedEventArgs(status))
        member this.Id = this.Id
        member this.Insert(index, item) = this.Insert(index, item)        
        [<CLIEvent>] member this.Changed = this.Changed        
        [<CLIEvent>] member this.Updated = this.Updated        
        [<CLIEvent>] member this.Removed = this.Removed        
        [<CLIEvent>] member this.Added = this.Added        
        [<CLIEvent>] member this.Cleared = this.Cleared        
        member this.Clear() = this.Clear()
        member this.Count = this.Count
        member this.RemoveAt(index) = this.RemoveAt(index)
        member this.IsReadOnly = this.IsReadOnly
        member this.Add(item) = this.Add(item)
        member this.Contains(item) = this.Contains(item)
        member this.CopyTo(array, arrayIndex) = this.CopyTo(array, arrayIndex)
        member this.Remove(item) = this.Remove(item)
        member this.GetEnumerator() = this.GetEnumerator()
        member this.Value with get () = this.Value :> IList<'U>

    interface INode<List<'U>> with
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