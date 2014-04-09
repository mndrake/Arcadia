namespace Arcadia.Cells

open System
open System.Collections.Generic
open System.ComponentModel
open System.Threading
open Microsoft.FSharp.Reflection
open Helpers

/// output cell used within a CalculationEngine
type OutputCell<'U>(id, cellInputs : ICell seq, cellFunction : unit -> 'U) as this =

    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()

    // convert tuple to object array
    let cells = cellInputs |> Seq.toArray
    
    let func = cellFunction

    let value = ref Unchecked.defaultof<'U>

    let mutable cts = new CancellationTokenSource()

    
    let status = ref CellStatus.Dirty
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    let newCts() = new CancellationTokenSource()
    let queue = List<Message>()
    
    let agent = 
        MailboxProcessor.Start(fun inbox -> 
            let rec nextMsg() = 
                async { 
                    // process incoming messages
                    while (queue.Count = 0 && inbox.CurrentQueueLength = 0) || inbox.CurrentQueueLength > 0 do
                        let! msg = inbox.Receive()
                        queue.Add(msg)
                    // process priority - Cancelled -> Changed -> otherwise order received
                    if queue.Contains(Cancelled) then 
                        queue.RemoveAll(fun m -> m = Cancelled) |> ignore
                        return Cancelled
                    elif queue.Contains(Changed) then 
                        queue.RemoveAll(fun m -> m = Changed) |> ignore
                        return Changed
                    else 
                        let msg = queue |> Seq.head
                        queue.RemoveAt(0)
                        return msg
                }
            
            let rec calculate() = 
                async { 
                    let! cellValues = Async.Parallel(cells |> Array.map(fun n -> n.Evaluate()))
                    let statuses, values = cellValues |> Array.unzip
                    if (statuses |> Array.forall(fun n -> n = CellStatus.Valid)) then 
                        if cts.IsCancellationRequested then cts <- newCts()
                        Async.StartWithContinuations
                            (async { 
                                let result = 
                                    try
                                        Some <| func()
                                    with
                                    | e -> System.Diagnostics.Debug.Print(e.ToString()); None
                                return result }, 
                             (fun v -> 
                                match v with
                                |Some v ->
                                    value := v
                                    inbox.Post(Processed)
                                |None ->
                                    inbox.Post(Error)), 
                             (fun _ -> 
                                inbox.Post(Error)),
                                (fun _ -> inbox.Post(Cancelled)), cts.Token)
                    return! processing()
                }
            
            and valid() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> 
                        status := CellStatus.Processing
                        changed.Trigger(this, ChangedEventArgs(CellStatus.Processing))
                        return! calculate()
                    | Eval r -> 
                        r.Reply(CellStatus.Valid, box !value)
                        return! valid()
                    | _ -> return! valid()
                }
            
            and processing() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! calculate()
                    | Eval r -> 
                        r.Reply(CellStatus.Processing, null)
                        return! processing()
                    | Error ->
                        status := CellStatus.Error
                        changed.Trigger(this, ChangedEventArgs(CellStatus.Error))
                        return! error()
                    | Processed -> 
                        status := CellStatus.Valid
                        changed.Trigger(this, ChangedEventArgs(CellStatus.Valid))
                        return! valid()
                    | Cancelled -> 
                        status := CellStatus.Dirty
                        changed.Trigger(this, ChangedEventArgs(CellStatus.Cancelled))
                        return! dirty()
                }
            and error() =
                async {
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! calculate()
                    | Eval r -> 
                        r.Reply(CellStatus.Error, null)
                        return! calculate()
                    | _ -> return! error() 
                }

            and dirty() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! dirty()
                    | Eval r -> 
                        r.Reply(CellStatus.Dirty, null)
                        return! calculate()
                    | _ -> return! dirty()
                }
            
            // initial state
            calculate()
            )
    do 
        cells |> Seq.iter(fun n -> 
                    n.Changed.Add(fun arg -> 
                        match arg.Status with
                        | CellStatus.Cancelled -> agent.Post(Cancelled)
                        | _ -> agent.Post(Changed)))
        (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))
        changed.Publish.Add(fun _ -> this.RaisePropertyChanged "Value")

    new (cellInputs, cellFunction) = OutputCell("", cellInputs, cellFunction) 
    
    abstract OnPropertyChanged : string -> unit
    override this.OnPropertyChanged(_) = ()

    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])

    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.GetDependentCells() = cells.Clone() |> unbox
    member this.Status = !status
    
    member this.Value 
        with get () = !value
        and set _ = failwith "cannot set the value of an output cell"
        
    member this.IsDirty = 
        match !status with
        | CellStatus.Valid -> false
        | _ -> true
    
    member this.IsProcessing = 
        match !status with
        | CellStatus.Processing -> true
        | _ -> false
    
    [<CLIEvent>]
    member this.Changed = changed.Publish    
    member this.IsInput = false
    member this.ToICell() = this :> ICell<'U>
    member this.Id = id

    interface ICell<'U> with
        member this.Status = this.Status
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.GetDependentCells() = this.GetDependentCells()
        member this.IsDirty = this.IsDirty
        member this.Evaluate() = this.Evaluate()
        member this.Id = this.Id
        member this.IsInput = this.IsInput
        member this.IsProcessing = this.IsProcessing
        member this.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v      
        member this.Value 
            with get () = this.Value
            and set v = this.Value <- v

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish