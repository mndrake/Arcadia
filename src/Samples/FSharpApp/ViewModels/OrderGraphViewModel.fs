namespace FSharpApp.ViewModels

open System.Linq
open Utopia
open Utopia.Graph
open Utopia.ViewModel


type OrderGraphVertex(node)  = inherit NodeVertexBase(node)

type OrderGraphViewModel(ce : ICalculationEngine) = 
    inherit PageViewModel()

    let graph = new NodeGraph(ce, fun n -> new OrderGraphVertex(n) :> INodeVertex)

    new() = new OrderGraphViewModel(new CalculationEngine())

    member this.BooleanTypes = [| true; false |]
    
    member this.AutoCalculate 
        with get () = ce.Calculation.Automatic
        and set v = ce.Calculation.Automatic <- v
    
    member this.LayoutAlgorithmType = "EfficientSugiyama"
    override this.Name = "OrderGraph"

    member this.Graph = graph
