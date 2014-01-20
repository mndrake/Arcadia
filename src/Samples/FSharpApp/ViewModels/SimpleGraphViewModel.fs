namespace FSharpApp.ViewModels

open System.Linq
open Arcadia
open Arcadia.Graph
open Arcadia.ViewModel

type SimpleGraphVertex(node) as this = 
    inherit NodeVertexBase(node)
    
    do node.Changed.Add(fun args -> this.RaisePropertyChanged "Value")

    member this.Value
        with get() : int = node.UntypedValue |> unbox
         and set (v : int) = node.UntypedValue <- box v

type SimpleGraphViewModel(ce : ICalculationEngine) = 
    inherit PageViewModel()
    let graph = new NodeGraph(ce, fun n -> new SimpleGraphVertex(n) :> INodeVertex)
    new() = new SimpleGraphViewModel(new CalculationEngine())
    member this.BooleanTypes = [| true; false |]
    
    member this.AutoCalculate 
        with get () = ce.Calculation.Automatic
        and set v = ce.Calculation.Automatic <- v
    
    member this.LayoutAlgorithmType = "EfficientSugiyama"
    override this.Name = "SimpleGraph"
    member this.CancelCalculateCommand = command <| fun () -> ce.Calculation.Cancel()
    member this.CalculateFullCommand = command <| fun () -> graph.UpdateNode("out9")
    member this.CalculatePartialCommand = command <| fun () -> graph.UpdateNode("out4")
    member this.CalculateSecondaryCommand = command <| fun () -> graph.UpdateNode("out10")
    member this.Graph = graph
