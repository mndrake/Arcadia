open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Diagnostics
open System.Linq
open System.Threading


/// checks if value is a F# Tuple type
let isTuple value = 
    match box value with
    | null -> false
    | _ -> Microsoft.FSharp.Reflection.FSharpType.IsTuple(value.GetType())

// http://stackoverflow.com/questions/2361851/c-sharp-and-f-casting-specifically-the-as-keyword
/// F# implementation of the C# 'as' keyword
let castAs<'T when 'T : null>(o : obj) = 
    match o with
    | :? 'T as res -> res
    | _ -> null

/// checks to see if object is of type 'T
let isType<'T> o =
    match box o with 
    | :? 'T -> true
    | _ -> false


/// the status of InputNodes and OutputNodes
type NodeStatus =
    | Dirty = 0
    | Processing = 1
    | Cancelled = 2
    | Error = 3
    | Valid = 4

/// event arguments for an objects Changed event
type ChangedEventArgs(status:NodeStatus) =
    inherit EventArgs()
    member this.Status = status

/// event handler for an objects Changed event
type ChangedEventHandler = delegate of sender:obj * e:ChangedEventArgs -> unit

/// node messages used in internal MailboxProcessor
type internal Message = 
    | Cancelled
    | Changed
    | Error
    | Eval of AsyncReplyChannel<NodeStatus * obj>
    | Processed
    | AutoCalculation of bool

/// interface of CalculationHandler
type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<EventHandler, EventArgs> with get
    abstract Cancel : unit -> unit
    abstract Reset : unit -> unit
    
/// untyped interface of InputNode and OutputNode
type INode = 
    abstract Calculation : ICalculationHandler with get
    abstract Evaluate : unit -> Async<NodeStatus * obj>
    abstract UntypedValue : obj with get, set
    abstract Id : string with get
    abstract Status : NodeStatus with get
    abstract AsyncCalculate : unit -> unit
    abstract IsDirty : bool with get
    abstract GetDependentNodes : unit -> INode []
    abstract IsInput : bool with get
    abstract IsProcessing : bool with get
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, ChangedEventArgs> with get

/// typed interface for InputNode and OutputNode
type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

/// interface for CalculationEngine
type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get


[<Sealed>]
type RecursiveModificationException() = 
    inherit Exception("Recursive modification of Setable")


type IEquate<'T> = 
    abstract EqualityComparer : ('T -> 'T -> bool) with get, set

type IGetable = 
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, ChangedEventArgs>

type IGetable<'T> = 
    inherit IGetable
    abstract Value : 'T with get
    abstract Status : NodeStatus with get

type IComputed<'T> = 
    abstract Dispose : unit -> unit
    abstract Value : 'T with get


type ISetable<'T> = 
    inherit IGetable<'T>
    abstract Value : 'T with set


/// Intended for use as a singleton, to allow broadcasting events to the
/// listeners at the top of the stack, per-thread
type ListenerStack<'T>() = 
    let Stack = new ThreadLocal<_>(fun () -> Stack<'T -> unit>())
    member this.Push(listener) = Stack.Value.Push(listener)
    member this.Pop() = Stack.Value.Pop()
    member this.Notify(obs) = if (Stack.Value.Count <> 0) then obs |> Stack.Value.Peek()


module ListenerStack =
    let EmptySubscriptions = new HashSet<IGetable>()
    let Listeners = new ListenerStack<IGetable>()


type Forwarder<'TImpl, 'TValue when 'TImpl :> IEquate<'TValue> and 'TImpl :> IGetable>(impl : 'TImpl) = 
    let _impl = impl
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    member this.Impl = _impl
    
    member this.Changed = changed.Publish
    
    member this.EqualityComparer 
        with get () = _impl.EqualityComparer
        and set v = _impl.EqualityComparer <- v
    
    interface IGetable with
        [<CLIEvent>]
        member I.Changed = changed.Publish
    
    interface IEquate<'TValue> with
        
        member I.EqualityComparer 
            with get () = _impl.EqualityComparer
            and set v = _impl.EqualityComparer <- v

/// handles calculation state for a CalculationEngine
type CalculationHandler() as this = 
    let changed = new Event<EventHandler, EventArgs>()
    let automatic = ref false

    let mutable cts = new CancellationTokenSource()

    member this.Reset() =
        cts <- new CancellationTokenSource()        
        
    member this.Automatic 
        with get () = !automatic
        and set v = 
            automatic := v
            this.Reset()
            changed.Trigger(this, EventArgs.Empty)
    
    member this.Cancel() = cts.Cancel()
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    interface ICalculationHandler with 
        member I.Automatic 
            with get () = this.Automatic
            and set v = this.Automatic <- v
        
        [<CLIEvent>]
        member I.Changed = this.Changed    
        member I.Cancel() = this.Cancel()       
        member I.CancellationToken with get() = cts.Token
        member I.Reset() = this.Reset()

/// input node used within a CalculationEngine
type Setable<'U>(calculationHandler : ICalculationHandler, ?initialValue : 'U) as this = 
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    let value = 
        match initialValue with
        | Some v -> ref v
        | None -> ref Unchecked.defaultof<'U>
    
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
    
    new (?initialValue) =
        match initialValue with
        | Some v -> Setable(new CalculationHandler(), v)
        | None -> Setable(new CalculationHandler(), Unchecked.defaultof<'U>)

    member val Id = "" with get,set
    
    member this.Value 
        with get () = 
            ListenerStack.Listeners.Notify(this)
            !value
        and set v = 
            if not <| this.EqualityComparer !value v then
                value := v
                changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
    
    member this.Calculation = calculationHandler
    
    [<CLIEvent>]
    member this.Changed = changed.Publish

    static member DefaultEqualityComparer a b =
        obj.ReferenceEquals(a, b) || (not <| obj.ReferenceEquals(a, null) && obj.Equals(a, b))

    member val EqualityComparer = Setable<_>.DefaultEqualityComparer with get, set

    interface IEquate<'U> with
        member this.EqualityComparer 
            with get () = this.EqualityComparer
            and set v = this.EqualityComparer <- v

    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)

    interface ISetable<'U> with
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.Value 
            with get() = this.Value
             and set v = this.Value <- v
        member this.Status = NodeStatus.Valid

type Computed<'U>(calculationHandler : ICalculationHandler, compute : unit -> 'U) as this =
    inherit Forwarder<Setable<'U>, 'U>(new Setable<'U>())
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    let mutable subscriptions = ListenerStack.EmptySubscriptions    
    let value = ref Unchecked.defaultof<'U>

    let mutable cts = 
        CancellationTokenSource.CreateLinkedTokenSource
            (calculationHandler.CancellationToken, new CancellationToken())

    let status = ref NodeStatus.Dirty
    let changed = new Event<ChangedEventHandler, ChangedEventArgs>()
    let newCts() = 
        CancellationTokenSource.CreateLinkedTokenSource(calculationHandler.CancellationToken, new CancellationToken())
    let queue = List<Message>()

//    let agent =
//        MailboxProcessor.Start(fun inbox -> 
//            let rec nextMsg() = 
//                async { 
//                    // process incoming messages
//                    while (queue.Count = 0 && inbox.CurrentQueueLength = 0) || inbox.CurrentQueueLength > 0 do
//                        let! msg = inbox.Receive()
//                        queue.Add(msg)
//                    // process priority - Cancelled -> Changed -> otherwise order received
//                    if queue.Contains(Cancelled) then 
//                        queue.RemoveAll(fun m -> m = Cancelled) |> ignore
//                        return Cancelled
//                    elif queue.Contains(Changed) then 
//                        queue.RemoveAll(fun m -> m = Changed) |> ignore
//                        return Changed
//                    else 
//                        let msg = queue |> Seq.head
//                        queue.RemoveAt(0)
//                        return msg
//                }
//
//            let handler = 
//                ChangedEventHandler(fun sender e -> 
//                    match e with
//                    | NodeStatus.Cancelled -> agent.Post(Cancelled)
//                    | _ -> agent.Post(Changed))
//
//            let rec calculate() =
//                async {
//                    
//                    let newSubscriptions = new HashSet<IGetable>()
//                    ListenerStack.Listeners.Push(fun (o : IGetable) -> newSubscriptions.Add(o) |> ignore)
//                    let newVal = compute()
//                    ListenerStack.Listeners.Pop() |> ignore
//                    this.Impl.Value <- newVal
//                    newSubscriptions.Remove(this) |> ignore
//                    newSubscriptions.Remove(this.Impl) |> ignore
//                    for sub in subscriptions |> Seq.filter (fun s -> not <| newSubscriptions.Contains(s)) do
//                        sub.Changed.RemoveHandler(handler)
//                    for sub in newSubscriptions.Where(fun s -> not <| subscriptions.Contains(s)) do
//                        sub.Changed.AddHandler(handler)
//                    subscriptions <- newSubscriptions
//               }

//            let rec calculate() = 
//                async { 
//                    let! nodeValues = Async.Parallel(nodes |> Array.map(fun n -> n.Evaluate()))
//                    let statuses, values = nodeValues |> Array.unzip
//                    if (statuses |> Array.forall(fun n -> n = NodeStatus.Valid)) then 
//                        if cts.IsCancellationRequested then cts <- newCts()
//                        Async.StartWithContinuations
//                            (async { 
//                                let result = 
//                                    try
//                                        Some <| func values
//                                    with
//                                    | e -> System.Diagnostics.Debug.Print(e.ToString()); None
//                                return result }, 
//                             (fun v -> 
//                                match v with
//                                |Some v ->
//                                    value := v
//                                    inbox.Post(Processed)
//                                |None ->
//                                    inbox.Post(Error)), 
//                             (fun _ -> 
//                                inbox.Post(Error)),
//                                (fun _ -> inbox.Post(Cancelled)), cts.Token)
//                    return! processing()
//                }
          
//               )
                    

//            
//            and valid() = 
//                async { 
//                    let! msg = nextMsg()
//                    match msg with
//                    | Changed -> 
//                        if not calculationHandler.Automatic then 
//                            status := NodeStatus.Dirty
//                            changed.Trigger(this, ChangedEventArgs(NodeStatus.Dirty))
//                            return! dirty()
//                        else 
//                            status := NodeStatus.Processing
//                            changed.Trigger(this, ChangedEventArgs(NodeStatus.Processing))
//                            return! calculate()
//                    | Eval r -> 
//                        r.Reply(NodeStatus.Valid, box !value)
//                        return! valid()
//                    | _ -> return! valid()
//                }
//            
//            and processing() = 
//                async { 
//                    let! msg = nextMsg()
//                    match msg with
//                    | Changed -> return! calculate()
//                    | Eval r -> 
//                        r.Reply(NodeStatus.Processing, null)
//                        return! processing()
//                    | Error ->
//                        status := NodeStatus.Error
//                        changed.Trigger(this, ChangedEventArgs(NodeStatus.Error))
//                        return! error()
//                    | Processed -> 
//                        status := NodeStatus.Valid
//                        changed.Trigger(this, ChangedEventArgs(NodeStatus.Valid))
//                        return! valid()
//                    | AutoCalculation _ -> return! processing()
//                    | Cancelled -> 
//                        status := NodeStatus.Dirty
//                        changed.Trigger(this, ChangedEventArgs(NodeStatus.Cancelled))
//                        return! dirty()
//                }
//            and error() =
//                async {
//                    let! msg = nextMsg()
//                    match msg with
//                    | Changed -> return! calculate()
//                    | Eval r -> 
//                        r.Reply(NodeStatus.Error, null)
//                        return! calculate()
//                    | _ -> return! error() 
//                }
//
//            and dirty() = 
//                async { 
//                    let! msg = nextMsg()
//                    match msg with
//                    | Changed -> return! dirty()
//                    | Eval r -> 
//                        r.Reply(NodeStatus.Dirty, null)
//                        return! calculate()
//                    | AutoCalculation true -> return! calculate()
//                    | _ -> return! dirty()
//                }
//            
//            // initial state
//            match calculationHandler.Automatic with
//            | true -> calculate()
//            | false -> dirty())

    let rec recompute() =
        let newSubscriptions = new HashSet<IGetable>()
        ListenerStack.Listeners.Push(fun (o : IGetable) -> newSubscriptions.Add(o) |> ignore)
        let newVal = compute()
        ListenerStack.Listeners.Pop() |> ignore
        this.Impl.Value <- newVal
        newSubscriptions.Remove(this) |> ignore
        newSubscriptions.Remove(this.Impl) |> ignore
        for sub in subscriptions |> Seq.filter (fun s -> not <| newSubscriptions.Contains(s)) do
            sub.Changed.RemoveHandler(fun _ _ -> recompute())
        for sub in newSubscriptions.Where(fun s -> not <| subscriptions.Contains(s)) do
            sub.Changed.AddHandler(fun _ _ -> recompute())
        subscriptions <- newSubscriptions
    do 
        recompute()

    member this.Subscriptions = subscriptions

    member this.Value
        with get():'U = this.Impl.Value
        and set ( _:'U) = failwith "cannot set the value of an output node"

    new (compute) = Computed(new CalculationHandler(), compute)
    
    member this.Status = !status
    
    member this.IsDirty = 
        match !status with
        | NodeStatus.Valid -> false
        | _ -> true
    
    member this.IsProcessing = 
        match !status with
        | NodeStatus.Processing -> true
        | _ -> false
    
    [<CLIEvent>]
    member this.Changed = changed.Publish    
    member this.IsInput = false

    member this.Calculation = calculationHandler
    member this.Id = id


module Test = 

    let in0 = Setable(2)
    let in1 = Setable(4)

    let out0 = Computed(fun () -> in0.Value + in1.Value)

    in0.Value <- 3

    out0.Value

    out0.Subscriptions

    in0.Changed.Add(fun arg -> printfn "changed")

