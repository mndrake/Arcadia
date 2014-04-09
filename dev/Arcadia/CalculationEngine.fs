namespace Arcadia

open System
open System.Collections.Generic
open System.Collections.ObjectModel

/// asynchronous calculation engine
type CalculationEngine(calculationHandler : ICalculationHandler) as this = 

    let nodes = Collection<INode>()

    let getNode(nodeId) = 
            nodes
            |> Seq.tryFind (fun n -> n.Id=nodeId)
            |> function
               | Some n -> n
               | None -> failwith "node not found"

    let mutable inputCount = 0
    let mutable outputCount = 0
//    let mutable isInitialized = false
//    let initialized = Event<EventHandler,EventArgs>()

    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
//    [<CLIEvent>]
//    member this.Initialized = initialized.Publish

//    member this.IsInitialized 
//        with get() = isInitialized
//         and private set v = 
//            if v then initialized.Trigger(null, EventArgs.Empty)
//            isInitialized <- v

//    member this.Initialize() =
//        let list = ref (nodes |> Seq.toArray)
//
//        let test() =
//            list := (!list) |> Array.filter (fun n -> n.Status = NodeStatus.Uninitialized)
//            (!list).Length > 0
//
//        async { let result = ref true
//                while !result do
//                    do! Async.Sleep(100)
//                    result := test()
//                do this.IsInitialized <- true } |> Async.Start

    member this.Calculation = calculationHandler

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    /// adds an InputNode to the CalculationEngine
    member this.Setable(value : 'U, nodeId : string) = 
        let input = Setable<'U>(this.Calculation, value, Id = nodeId)
        nodes.Add(input)
        input

    /// adds an InputNode to the CalculationEngine
    member this.Setable(value : 'U) =
        let nodeId = "in" + string inputCount
        inputCount <- inputCount + 1
        this.Setable(value, nodeId)

    /// adds an OutputNode to the CalculationEngine    
    member this.Computed(nodeFunction : Func<'U>, nodeId : string) =
        let output = new Computed<'U>(this.Calculation, nodeFunction, Id = nodeId)
        nodes.Add(output)
        output

    /// adds an OutputNode to the CalculationEngine    
    member this.Computed(nodeFunction : Func<'U>, nodeId : string, throttle : int) =
        let output = new Computed<'U>(this.Calculation, nodeFunction, Id = nodeId)
        nodes.Add(output)
        output.Throttle(throttle)


    /// adds an OutputNode to the CalculationEngine
    member this.Computed(nodeFunction : Func<'U>) =
        let nodeId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.Computed(nodeFunction, nodeId)

    /// adds an OutputNode to the CalculationEngine
    member this.Computed(nodeFunction : Func<'U>, throttle : int) =
        let nodeId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.Computed(nodeFunction, nodeId).Throttle(throttle)

    /// get node by id
    member this.Node<'U>(nodeId) = getNode nodeId :?> INode<'U>

    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
//        [<CLIEvent>]
//        member I.Initialized = this.Initialized
//        member I.IsInitialized = this.IsInitialized
    
    /// evaluates a given calculation node asynchronously
    static member Evaluate(node : IGetable) = async { node.Evaluate() |> ignore } |> Async.Start