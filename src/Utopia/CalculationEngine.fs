namespace Utopia

open System
open System.Collections.Generic
open Utopia.ViewModel

type NodeFunc<'T, 'U> = delegate of 'T -> 'U

type CalculationEngine(calculationHandler : ICalculationHandler) as this = 

    let nodes = List<INode>()
    let mutable inputCount = 0
    let mutable outputCount = 0
    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
    member this.Calculation = calculationHandler

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    member this.AddInput(value : 'U, nodeID : string) = 
        let input = InputNode<'U>(this.Calculation, nodeID, value)
        nodes.Add(input)
        input

    member this.AddInput(value : 'U) =
        let nodeID = "in" + string inputCount
        inputCount <- inputCount + 1
        this.AddInput(value, nodeID)
    
    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>, nodeID : string) =
        let f(t) = nodeFunction.Invoke(t)
        let output = OutputNode<'N, 'T, 'U>(this.Calculation, nodeID, dependentNodes, f)
        nodes.Add(output)
        output

    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>) =
        let nodeID = "out" + string outputCount
        outputCount <- outputCount + 1
        this.AddOutput(dependentNodes, nodeFunction, "out" + string outputCount)
 
    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
    
    static member Eval(node : INode) = async { node.Eval |> ignore } |> Async.Start