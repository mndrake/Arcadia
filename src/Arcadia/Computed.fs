#nowarn "40"
namespace Arcadia

open System
open System.Collections.Generic
open System.ComponentModel
open System.Linq
open System.Threading
open Microsoft.FSharp.Reflection
open Helpers

[<Sealed>]
/// output node used within a CalculationEngine
type Computed<'U>(calculationHandler : ICalculationHandler, compute : Func<'U>) as this =
    inherit Forwarder<Setable<'U>,'U>(new Setable<'U>(calculationHandler, Unchecked.defaultof<'U>))
    let mutable throttledRecompute = Action(fun() -> this.Recompute())
    let newCts() = 
        CancellationTokenSource.CreateLinkedTokenSource
            (this.Impl.Calculation.CancellationToken, new CancellationToken())
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    let subscriptions = ref ComputedStack.EmptySubscriptions
    let mutable cts = newCts()
    //let mutable isInitialized = false
    let queue = List<Message>()

    let rec recompute() =
        let newSubscriptions = new HashSet<_>()
        ComputedStack.Listeners.Push(fun o -> newSubscriptions.Add(o) |> ignore)
        let newVal = compute.Invoke()
        ComputedStack.Listeners.Pop() |> ignore
        newSubscriptions.Remove(this.Impl) |> ignore
        for sub in !subscriptions |> Seq.filter (fun s -> not <| newSubscriptions.Contains(s)) do
            sub.Changed.RemoveHandler(changeHandler)
        for sub in newSubscriptions.Where(fun s -> not <| (!subscriptions).Contains(s)) do
            sub.Changed.AddHandler(changeHandler)
        subscriptions := newSubscriptions
        newVal
    
    and changeHandler : ChangedEventHandler = 
                ChangedEventHandler(fun sender arg -> 
                    match arg.Status with
                    | NodeStatus.Cancelled -> agent.Post(Cancelled)
                    | _ -> this.RecomputeSoon())
                        
    and agent : MailboxProcessor<Message> = 
        MailboxProcessor.Start(fun inbox -> 
            let rec nextMsg() = 
                async { 
                    // process incoming messages
                    while (queue.Count = 0 && inbox.CurrentQueueLength = 0) || inbox.CurrentQueueLength > 0 do
                        let! msg = inbox.Receive()
                        queue.Add(msg)
                    // process priority : Cancelled -> Changed -> Processed -> otherwise order received
                    if queue.Contains(Cancelled) then 
                        queue.RemoveAll(fun m -> m = Cancelled) |> ignore
                        return Cancelled
                    elif queue.Contains(Changed) then 
                        queue.RemoveAll(fun m -> m = Changed) |> ignore
                        return Changed
                    elif queue.Contains(Processed) then
                        queue.RemoveAll(fun m -> m = Processed) |> ignore
                        return Processed
                    else 
                        let msg = queue |> Seq.head
                        queue.RemoveAt(0)
                        return msg
                }



            let rec calculate() = 
                async { 
                    let! nodeValues = Async.Parallel(!subscriptions |> Seq.map(fun n -> n.Evaluate()))
                    let statuses, _ = nodeValues |> Array.unzip
                    if (statuses |> Array.forall(fun n -> n = NodeStatus.Valid)) then 
                        if cts.IsCancellationRequested then cts <- newCts()
                        Async.StartWithContinuations
                            (async { 
                                let result = 
                                    try
                                        Some <| recompute() 
                                    with
                                    | e -> System.Diagnostics.Debug.Print(e.ToString());None
                                return result }, 
                             (fun v -> 
                                match v with
                                |Some v ->
                                    this.Impl.SetValue v
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
                        if not this.Impl.Calculation.Automatic then 
                            this.Impl.Status <- NodeStatus.Dirty
                            this.OnChanged(NodeStatus.Dirty)
                            return! dirty()
                        else 
                            this.Impl.Status <- NodeStatus.Processing
                            this.OnChanged(NodeStatus.Processing)
                            return! calculate()
                    | Eval r -> 
                        r.Reply(NodeStatus.Valid, box this.Value)
                        return! valid()
                    | _ -> return! valid()
                }
            
            and processing() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! calculate()
                    | Eval r -> 
                        r.Reply(NodeStatus.Processing, null)
                        return! processing()
                    | Error ->
                        this.Impl.Status <- NodeStatus.Error
                        this.OnChanged(NodeStatus.Error)
                        return! error()
                    | Processed -> 
                        this.Impl.Status <- NodeStatus.Valid
                        this.OnChanged(NodeStatus.Valid)
                        return! valid()
                    | AutoCalculation _ -> return! processing()
                    | Cancelled -> 
                        this.Impl.Status <- NodeStatus.Dirty
                        this.OnChanged(NodeStatus.Cancelled)
                        return! dirty()
                }
            and error() =
                async {
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! calculate()
                    | Eval r -> 
                        r.Reply(NodeStatus.Error, null)
                        return! calculate()
                    | _ -> return! error() 
                }

            and dirty() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> return! dirty()
                    | Eval r -> 
                        r.Reply(NodeStatus.Dirty, null)
                        return! calculate()
                    | AutoCalculation true -> return! calculate()
                    | _ -> return! dirty()
                }
            
            // initial state
            match this.Impl.Calculation.Automatic with
            | true -> calculate()
            | false -> dirty()  
        )
    
    do 
        this.Impl.Status <- NodeStatus.Uninitialized

        this.Impl.EvaluateSet (fun () -> agent.PostAndAsyncReply(fun r -> Eval r))
        this.Impl.Calculation.Changed.Add(fun _ -> agent.Post(AutoCalculation(this.Impl.Calculation.Automatic)))
        (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))
        this.Changed.Add(fun _ -> this.RaisePropertyChanged "Value")

        // initialize

        if not <| isType<'U>(()) then 
            ignore <| recompute()
            this.Impl.Status <- NodeStatus.Dirty
            this.Impl.OnChanged(NodeStatus.Dirty)

    new (compute) = new Computed<'U>(CalculationHandler(Automatic = true), compute)
    
    abstract OnPropertyChanged : string -> unit
    default this.OnPropertyChanged(_) = ()

    member private this.RecomputeSoon() = throttledRecompute.Invoke()    
    member private this.Recalculate() = agent.Post(Changed)

    member this.Id 
        with get() = this.Impl.Id
         and set v = this.Impl.Id <- v

    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])

    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.GetDependentNodes() : INode[] = !subscriptions |> Seq.cast<obj> |> Seq.map(unbox) |> Seq.toArray
    member this.Status = this.Impl.Status
    
    member this.Value 
        with get() = this.Impl.Value
         and set _ = failwith "cannot set the value of an output node"
    
    member this.Calculation = this.Impl.Calculation

    member this.AsyncCalculate() = 
        if this.Impl.Calculation.CancellationToken.IsCancellationRequested then
            this.Impl.Calculation.Reset()
        cts <- CancellationTokenSource.CreateLinkedTokenSource(
                        this.Impl.Calculation.CancellationToken, new CancellationToken())
        Async.Start((async { do! this.Evaluate() |> Async.Ignore }), cts.Token)


    member private this.Recompute = fun() -> agent.Post(Message.Changed)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish
    
    interface IGetable<'U> with
        member this.Value = this.Value
    
    interface IDisposable with
        member this.Dispose() = 
            for node in !subscriptions do node.Changed.RemoveHandler(changeHandler)

    interface ICanThrottle<Computed<'U>> with
        member this.SetThrottler(throttler : Func<Action,Action>) = 
            throttledRecompute <- throttler.Invoke(Action(fun () -> this.Recompute()))
            this  
            
    member this.ToINode() = this :> INode<'U>

    interface INode<'U> with
        member this.AsyncCalculate() = this.AsyncCalculate()
        member this.Calculation = this.Calculation
        member this.Status = this.Status
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.GetDependentNodes() = this.GetDependentNodes()
        member this.IsDirty =
            match this.Impl.Status with
            | NodeStatus.Valid -> false
            | _ -> true
        member this.IsProcessing =
            match this.Impl.Status with
            |NodeStatus.Processing -> true
            | _ -> false
        member this.Evaluate() = this.Evaluate()
        member this.Id = this.Id
        member this.IsInput = false
        member this.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v      
        member this.Value 
            with get () = this.Value
            and set v = this.Value <- v      

    static member op_Implicit(from : Computed<'U>) = from.Value

[<Sealed>] 
type Computed =  
    static member Do(compute : Action) = new Computed<unit>(fun() ->compute.Invoke())
    static member From<'T>(compute : Func<'T>) : Computed<'T> = new Computed<'T>(compute)
//    static member From<'T>(get : Func<'T>, set : Action<'T>) = new SetableComputed<'T>(get, set)
//    static member From<'T>(asyncObs : Func<Async<'T>>) = new AsyncComputed<'T>(asyncObs)