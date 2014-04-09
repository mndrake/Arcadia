namespace Arcadia

open System
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
    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
    member this.Calculation = calculationHandler

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    /// adds an InputNode to the CalculationEngine
    member this.AddInput(value : 'U, nodeId : string) = 
        let input = InputNode<'U>(this.Calculation, nodeId, value)
        nodes.Add(input)
        input

    /// adds an InputNode to the CalculationEngine
    member this.AddInput(value : 'U) =
        let nodeId = "in" + string inputCount
        inputCount <- inputCount + 1
        this.AddInput(value, nodeId)

    /// adds an OutputNode to the CalculationEngine    
    member this.AddOutput(dependentNodes : 'N, nodeFunction : Func<'T,'U>, nodeId : string) =
        let f(t) = nodeFunction.Invoke(t)
        let output = OutputNode<'N, 'T, 'U>(this.Calculation, nodeId, dependentNodes, f)
        nodes.Add(output)
        output

    /// adds an OutputNode to the CalculationEngine
    member this.AddOutput(dependentNodes : 'N, nodeFunction : Func<'T,'U>) =
        let nodeId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.AddOutput(dependentNodes, nodeFunction, nodeId)

    /// get node by id
    member this.Node<'U>(nodeId) = getNode nodeId :?> INode<'U>

    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
    
    /// evaluates a given calculation node asynchronously
    static member Evaluate(node : INode) = async { node.Evaluate() |> ignore } |> Async.Start