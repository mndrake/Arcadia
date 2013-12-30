namespace Utopia

open System
open System.Collections.Generic

type CalculationEngine(calculationHandler : ICalculationHandler) as this = 
    let nodes = List<INode>()
    let mutable inputCount = 0
    let mutable outputCount = 0
    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
    member this.Calculation = calculationHandler
    
    member this.AddInput(value : 'U, ?nodeID : string) = 
        let id = 
            match nodeID with
            | Some i -> i
            | None -> "in" + string inputCount
        inputCount <- inputCount + 1
        let input = InputNode<'U>(this.Calculation, id, value)
        nodes.Add(input)
        input
    
    member this.AddOutput(dependentNodes : 'N, nodeFunction : 'T -> 'U, ?nodeID : string) = 
        let id = 
            match nodeID with
            | Some i -> i
            | None -> "out" + string outputCount
        outputCount <- outputCount + 1
        let output = OutputNode<'N, 'T, 'U>(this.Calculation, dependentNodes, nodeFunction, id)
        nodes.Add(output)
        output
    
    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
    
    static member Eval(node : INode) = async { node.Eval |> ignore } |> Async.Start