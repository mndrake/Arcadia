namespace Arcadia

open System
open System.Collections.Generic
open System.Threading
open Microsoft.FSharp.Reflection
open Helpers

/// output node used within a CalculationEngine
type OutputNode<'N, 'T, 'U>(calculationHandler, id, nodeInputs : 'N, nodeFunction : 'T -> 'U, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)

    // convert tuple to object array
    let nodes = 
        if isTuple nodeInputs then FSharpValue.GetTupleFields(nodeInputs) |> Array.map(fun x -> x :?> INode)
        else [| (box nodeInputs) :?> INode |]
    
    let func = 
        if isTuple nodeInputs then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> nodeFunction
        else fun p -> (p.[0] :?> 'T) |> nodeFunction
    
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
                        if this.cts.IsCancellationRequested then this.cts <- newCts()
                        Async.StartWithContinuations
                            (async { return func values }, 
                             (fun v -> 
                             this.value := v
                             inbox.Post(Processed)), (fun _ -> ()), (fun _ -> inbox.Post(Cancelled)), this.cts.Token)
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
                        r.Reply(NodeStatus.Valid, box !this.value)
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
    
    override this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    override this.GetDependentNodes() = nodes.Clone() |> unbox
    override this.Status = !status
    
    override this.Value 
        with get () = !this.value
        and set v = failwith "cannot set the value of an output node"
        
    override this.IsDirty = 
        match !status with
        | NodeStatus.Valid -> false
        | _ -> true
    
    override this.IsProcessing = 
        match !status with
        | NodeStatus.Processing -> true
        | _ -> false
    
    [<CLIEvent>]
    override this.Changed = changed.Publish
    
    [<CLIEvent>]
    override this.Cancelled = cancelled.Publish
    
    override this.IsInput = false