namespace Arcadia

open System
open System.Collections.Generic
open System.Threading
open Microsoft.FSharp.Reflection
open Helpers

/// output node used within a CalculationEngine
type OutputNode<'N, 'T, 'U>(calculationHandler : ICalculationHandler, id, nodeInputs : 'N, nodeFunction : 'T -> 'U, ?initialValue) as this =

    // convert tuple to object array
    let nodes = 
        if isTuple nodeInputs then FSharpValue.GetTupleFields(nodeInputs) |> Array.map(fun x -> x :?> INode)
        else [| (box nodeInputs) :?> INode |]
    
    let func = 
        if isTuple nodeInputs then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> nodeFunction
        else fun p -> (p.[0] :?> 'T) |> nodeFunction

    let value = match initialValue with
                | Some v -> ref v
                | None -> ref Unchecked.defaultof<'U>

    let mutable cts = 
        CancellationTokenSource.CreateLinkedTokenSource(calculationHandler.CancellationToken, new CancellationToken())

    
    let status = ref NodeStatus.Dirty
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let cancelled = new Event<CancelledEventHandler, EventArgs>()
    let newCts() = 
        CancellationTokenSource.CreateLinkedTokenSource(calculationHandler.CancellationToken, new CancellationToken())
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
                        printfn "Cancelled"
                        return Cancelled
                    elif queue.Contains(Changed) then 
                        queue.RemoveAll(fun m -> m = Changed) |> ignore
                        printfn "Changed"
                        return Changed
                    else 
                        let msg = queue |> Seq.head
                        queue.RemoveAt(0)
                        printfn "%A" msg
                        return msg
                }
            
            let rec calculate() = 
                async { 
                    let! nodeValues = Async.Parallel(nodes |> Array.map(fun n -> n.Evaluate()))
                    let statuses, values = nodeValues |> Array.unzip
                    if (statuses |> Array.forall(fun n -> n = NodeStatus.Valid)) then 
                        if cts.IsCancellationRequested then cts <- newCts()
                        Async.StartWithContinuations
                            (async { return func values }, 
                             (fun v -> 
                             value := v
                             inbox.Post(Processed)), (fun _ -> ()), (fun _ -> inbox.Post(Cancelled)), cts.Token)
                    return! processing()
                }
            
            and valid() = 
                async { 
                    let! msg = nextMsg()
                    match msg with
                    | Changed -> 
                        if not calculationHandler.Automatic then 
                            status := NodeStatus.Dirty
                            changed.Trigger(this, EventArgs.Empty)
                            return! dirty()
                        else 
                            status := NodeStatus.Processing
                            changed.Trigger(this, EventArgs.Empty)
                            return! calculate()
                    | Eval r -> 
                        r.Reply(NodeStatus.Valid, box !value)
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
                    | Processed -> 
                        status := NodeStatus.Valid
                        changed.Trigger(null, EventArgs.Empty)
                        return! valid()
                    | AutoCalculation _ -> return! processing()
                    | Cancelled -> 
                        status := NodeStatus.Dirty
                        cancelled.Trigger(null, EventArgs.Empty)
                        return! dirty()
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
            match calculationHandler.Automatic with
            | true -> calculate()
            | false -> dirty())
    
    do 
        nodes |> Seq.iter(fun n -> n.Changed.Add(fun _ -> agent.Post(Changed)))
        nodes |> Seq.iter(fun n -> n.Cancelled.Add(fun _ -> agent.Post(Cancelled)))
        calculationHandler.Changed.Add(fun _ -> agent.Post(AutoCalculation(calculationHandler.Automatic)))

    new (calculationHandler, nodeInputs, nodeFunction) = OutputNode(calculationHandler, "", nodeInputs, nodeFunction) 
    
    member this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.GetDependentNodes() = nodes.Clone() |> unbox
    member this.Status = !status
    
    member this.Value 
        with get () = !value
        and set _ = failwith "cannot set the value of an output node"
        
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
    [<CLIEvent>]
    member this.Cancelled = cancelled.Publish    
    member this.IsInput = false
    member this.AsyncCalculate() = 
        if calculationHandler.CancellationToken.IsCancellationRequested then
            calculationHandler.Reset()
        cts <- CancellationTokenSource.CreateLinkedTokenSource(
                        calculationHandler.CancellationToken, new CancellationToken())
        Async.Start((async { do! this.Evaluate() |> Async.Ignore }), cts.Token)

    member this.ToINode() = this :> INode<'U>
    member this.Calculation = calculationHandler
    member this.Id = id

    interface INode<'U> with
        member this.AsyncCalculate() = this.AsyncCalculate()
        member this.Calculation = this.Calculation
        member this.Status = this.Status
        [<CLIEvent>]
        member this.Changed = this.Changed
        [<CLIEvent>]
        member this.Cancelled = this.Cancelled
        member this.GetDependentNodes() = this.GetDependentNodes()
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