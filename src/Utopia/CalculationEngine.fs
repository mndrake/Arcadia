namespace Utopia

open System
open System.Collections.ObjectModel

type NodeFunc<'T, 'U> = delegate of 'T -> 'U

type CalculationEngine(calculationHandler : ICalculationHandler) as this = 

    let nodes = Collection<INode>()
    let mutable inputCount = 0
    let mutable outputCount = 0
    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
    member this.Calculation = calculationHandler

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    member this.AddInput(value : 'U, nodeId : string) = 
        let input = InputNode<'U>(this.Calculation, nodeId, value)
        nodes.Add(input)
        input

    member this.AddInput(value : 'U) =
        let nodeId = "in" + string inputCount
        inputCount <- inputCount + 1
        this.AddInput(value, nodeId)
    
    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>, nodeId : string) =
        let f(t) = nodeFunction.Invoke(t)
        let output = OutputNode<'N, 'T, 'U>(this.Calculation, nodeId, dependentNodes, f)
        nodes.Add(output)
        output

    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>) =
        let nodeId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.AddOutput(dependentNodes, nodeFunction, nodeId)
 
    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
    
    static member Evaluate(node : INode) = async { node.Evaluate() |> ignore } |> Async.Start