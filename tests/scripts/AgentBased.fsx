open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Linq
open System.Threading
open Microsoft.FSharp.Control
open Microsoft.FSharp.Reflection

type ChangedEventHandler = delegate of sender:obj * e:EventArgs -> unit

type NodeStatus =
    | Dirty = 0
    | Processing = 1
    | Valid = 2

type Message = 
    | Cancelled
    | Changed
    | Eval of AsyncReplyChannel<NodeStatus * obj>
    | Processed
    | AutoCalculation of bool

//#region helpers

/// checks if value is a F# Tuple type
let isTuple value = 
    match box value with
    | null -> false
    | _ -> Microsoft.FSharp.Reflection.FSharpType.IsTuple(value.GetType())

/// basic message logger for calculation nodes
type Log() =
    let messages = List<_>()
    let toScreen = ref false

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec loop() = async {
                let! msg = inbox.Receive()
                match msg with
                |(name, state, message) -> 
                    let m = sprintf "STATE: %s MESSAGE: %A" state message
                    if !toScreen then printfn "NODE: %s %s" name m
                    messages.Add(name, m)
                return! loop()
                }
            loop())
    
    member this.ToScreen with get() = !toScreen and set v = toScreen := v
    member this.Post(name, state,message) = agent.Post(name,state,message)
    member this.Get(name) = 
        messages |> Seq.filter(fun (n,_) -> name = n)
                    |> Seq.map(fun (_,m) -> m)
                    |> Seq.iter (printfn "%s")
    member this.Get() = 
        messages |> Seq.iter (fun (name, message) -> printfn "NODE: %s %s" name message)

//#endregion

type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Cancel : unit -> unit
    abstract Reset : unit -> unit
    
type INode = 
    abstract Calculation : ICalculationHandler with get
    abstract Evaluate : unit -> Async<NodeStatus * obj>
    abstract UntypedValue : obj with get
    abstract Id : string with get
    abstract Status : NodeStatus with get
    abstract AsyncCalculate : unit -> unit
    abstract IsDirty : bool with get
    abstract GetDependentNodes : unit -> INode []
    abstract IsInput : bool with get
    abstract IsProcessing : bool with get
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract OnChanged : Action -> unit
    abstract OnCancelled : Action -> unit

type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

type CalculationHandler() = 
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let automatic = ref false
    let cts = ref <| new CancellationTokenSource()
    member this.Automatic 
        with get () = !automatic
        and set v = 
            if cts.Value.IsCancellationRequested then this.Reset()
            automatic := v
            changed.Trigger(null, EventArgs.Empty)
    member this.Cancel() = cts.Value.Cancel()
    member this.Reset() = 
        cts := new CancellationTokenSource()
        changed.Trigger(null, EventArgs.Empty)
    member this.Changed = changed.Publish
    member this.CancellationToken = cts.Value.Token

    interface ICalculationHandler with
        member this.CancellationToken with get() = this.CancellationToken
        member this.Automatic with get() = this.Automatic and set v = this.Automatic <- v
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.Cancel() = this.Cancel()
        member this.Reset() = this.Reset()


type Input<'U>(nodeId, log : Log, calc : ICalculationHandler, initialValue : 'U) = 
    let onChanged = List<Action>()
    let value = ref initialValue
    let changed = new Event<ChangedEventHandler, EventArgs>()
    
    let agent = 
        MailboxProcessor.Start(fun inbox -> 

            let rec valid() = 
                async { 
                    let! msg = inbox.Receive()
                    log.Post(nodeId, "valid", msg)
                    match msg with
                    | Eval r -> r.Reply(NodeStatus.Valid, box !value)
                    | Processed -> for action in onChanged do action.Invoke()
                                   changed.Trigger(null, EventArgs.Empty)
                    | _ -> ()
                    return! valid()
                    }
            valid())
    
    member this.SetValue v = 
        value := v
        agent.Post(Processed)
    
    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.Id = nodeId
    member this.OnChanged(action) = onChanged.Add(action)
    member this.Value with get() = !value
                       and set v = value := v
                                   agent.Post(Processed)
    member this.Status = NodeStatus.Valid
    member this.Calculation = calc
    [<CLIEvent>]
    member this.Changed = changed.Publish

    interface INode<'U> with
        member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
        member this.Id = nodeId
        member this.Status = NodeStatus.Valid
        member this.OnCancelled(action) = ()
        member this.OnChanged(action) = onChanged.Add(action)
        member this.UntypedValue = box !value
        member this.Calculation = calc
        [<CLIEvent>]
        member this.Changed = changed.Publish
        member this.AsyncCalculate() = ()
        member this.IsDirty = false
        member this.GetDependentNodes() = [||]
        member this.IsInput = true
        member this.IsProcessing = false
        member this.Value with get() = this.Value
                           and set v = this.Value <- v

type Output<'N, 'T, 'U>(nodeId, log : Log, calc : ICalculationHandler, nodeInputs : 'N, nodeFunction : 'T -> 'U) = 
    
    // convert tuple to object array

    let status = ref NodeStatus.Dirty

    let nodes = 
        if isTuple nodeInputs then FSharpValue.GetTupleFields(nodeInputs) |> Array.map(fun x -> x :?> INode)
        else [| (box nodeInputs) :?> INode |]
    
    let func = 
        if isTuple nodeInputs then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> nodeFunction
        else fun p -> (p.[0] :?> 'T) |> nodeFunction
    
    let value = ref(Unchecked.defaultof<'U>)
    let onCancelled = List<Action>()
    let onChanged = List<Action>()
    let changed = new Event<ChangedEventHandler, EventArgs>()

    let newCts() = CancellationTokenSource.CreateLinkedTokenSource(calc.CancellationToken, new CancellationToken())   

    let mutable cts = newCts()
    
    let queue = List<Message>()

    let agent = 
        MailboxProcessor.Start(fun inbox -> 

            let rec nextMsg() = async {
                // process incoming messages
                while (queue.Count = 0 && inbox.CurrentQueueLength = 0) || 
                      inbox.CurrentQueueLength > 0 do
                    let! msg = inbox.Receive()
                    queue.Add(msg)

                // process Changed messages first, but only need to process as one
                if queue.Contains(Changed) then
                    queue.RemoveAll(fun m -> m=Changed) |> ignore
                    return Changed
                else
                    // process all other requests in order
                    let msg = queue.First()
                    queue.RemoveAt(0)
                    return msg
                }
            
            let rec calculate() = async {
                let! nodeValues = Async.Parallel(nodes |> Array.map(fun n -> n.Evaluate()))
                let statuses, values = nodeValues |> Array.unzip
                if (statuses |> Array.forall(fun n -> n = NodeStatus.Valid)) then
                    if cts.IsCancellationRequested then
                       cts <- newCts()
                    Async.StartWithContinuations(
                        async {return func values},
                        (fun v -> value := v
                                  inbox.Post(Processed)),
                        (fun _ -> ()),
                        (fun _ -> inbox.Post(Cancelled)),
                        cts.Token)
                return! processing()
                }
            
            and valid() = async { 
                status := NodeStatus.Valid
                let! msg = nextMsg()
                log.Post(nodeId, "valid", msg)
                match msg with
                | Changed ->
                    if not calc.Automatic then
                        for action in onChanged do action.Invoke()
                        changed.Trigger(null, EventArgs.Empty)
                        return! dirty()
                    else 
                        for action in onChanged do action.Invoke()
                        changed.Trigger(null, EventArgs.Empty)                    
                        return! calculate()                    
                | Eval r -> 
                    r.Reply(NodeStatus.Valid, box !value)
                    return! valid()
                | _ -> 
                    return! valid()
                }
            
            and processing() = async { 
                status := NodeStatus.Processing
                let! msg = nextMsg()
                log.Post(nodeId, "processing", msg)
                match msg with
                | Changed -> 
                    return! calculate()
                | Eval r -> 
                    r.Reply(NodeStatus.Processing, null)
                    return! processing()
                | Processed -> 
                    for action in onChanged do action.Invoke()
                    changed.Trigger(null, EventArgs.Empty)
                    return! valid()
                | AutoCalculation _ -> 
                    return! processing()
                | Cancelled ->
                    for action in onCancelled do action.Invoke()
                    return! dirty()
                }
            
            and dirty() = 
                async { 
                    status := NodeStatus.Dirty
                    let! msg = nextMsg()
                    log.Post(nodeId, "dirty", msg)
                    match msg with
                    | Changed ->
                        return! dirty()
                    | Eval r -> 
                        r.Reply(NodeStatus.Dirty, null)
                        return! calculate()
                    | AutoCalculation true ->
                        return! calculate()
                    | _ -> return! dirty()
                    }
            
            // initial state
            if calc.Automatic then
                calculate()
            else
                dirty())
    
    do 
        nodes |> Seq.iter(fun n -> n.OnChanged(fun () -> agent.Post(Changed)))
        nodes |> Seq.iter(fun n -> n.OnCancelled(fun () -> agent.Post(Cancelled)))
        calc.Changed.Add(fun _ -> agent.Post(AutoCalculation(calc.Automatic)))
    
    member this.CancellationToken = cts.Token
    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.Id = nodeId
    member this.Status = !status
    member this.OnCancelled(action) = onCancelled.Add(action)
    member this.OnChanged(action) = onChanged.Add(action)
    member this.Value with get() = !value
    member this.Calculation = calc
    member this.AsyncCalculate() = async { this.Evaluate() |> ignore } |> Async.Start
    member this.IsDirty = 
        match !status with
        |NodeStatus.Dirty -> true
        |_ -> false
    member this.IsProcessing = 
        match !status with
        |NodeStatus.Processing -> true
        |_ -> false
    [<CLIEvent>]
    member this.Changed = changed.Publish

    interface INode<'U> with
        member this.OnCancelled(action) = onCancelled.Add(action)
        member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
        member this.Id = nodeId
        [<CLIEvent>]
        member this.Changed = changed.Publish
        member this.Status = !status
        member this.OnChanged(action) = onChanged.Add(action)
        member this.UntypedValue = box !value
        member this.Calculation = calc
        member this.AsyncCalculate() = this.AsyncCalculate()
        member this.IsDirty = this.IsDirty
        member this.GetDependentNodes() = nodes.Clone() |> unbox
        member this.IsInput = false
        member this.IsProcessing = this.IsProcessing
        member this.Value with get() = this.Value and set v = failwith "cannot set the value of an output node"

// ---------------------------------------------------------------------------------
// example

let log = Log()
let calc = CalculationHandler()

calc.Automatic <- false

let input name v = Input(name, log, calc, v)
let addNode name nodes = 
    Output(name, log, calc, nodes, 
           (fun (x, y) -> 
           log.Post(name, sprintf "eval %s, thread %i" name Thread.CurrentThread.ManagedThreadId, Processed)
           Thread.Sleep 1000
           x + y))

let eval(node : INode) = async { node.Evaluate() |> ignore } |> Async.Start

let i1 = input "i1" 1
let i2 = input "i2" 3
let i3 = input "i3" 5

let n1 = addNode "n1" (i1,i2)
let n2 = addNode "n2" (i2,i3)
let n3 = addNode "n3" (n1,n2)

(*

// test cancellation

// send log output to FSI
log.ToScreen <- true


// start evaluating node "n3"
//eval n3
calc.Automatic <- true

// wait
Thread.Sleep 1500

// cancel
calc.Cancel()

// wait
Thread.Sleep 500

// reset
calc.Reset()

*)

// run the following a line at a time

// enable automatic calculations
// calc.Automatic <- true

// print the messages log
// log.Get()