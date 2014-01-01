namespace FSharpApp.ViewModel

open System.Linq
open Utopia
open Utopia.ViewModel

type GraphViewModel(ce : ICalculationEngine) = 
    inherit PageViewModel()
    let graph = new CustomGraph(ce)
    new() = new GraphViewModel(new CalculationEngine())
    member this.BooleanTypes = [| true; false |]
    
    member this.AutoCalculate 
        with get () = ce.Calculation.Automatic
        and set v = ce.Calculation.Automatic <- v
    
    member this.LayoutAlgorithmType = "EfficientSugiyama"
    override this.Name = "Graph"
    member this.CancelCalculateCommand = command <| fun () -> ce.Calculation.Cancel()
    member this.CalculateFullCommand = command <| fun () -> graph.UpdateNode("out9")
    member this.CalculatePartialCommand = command <| fun () -> graph.UpdateNode("out4")
    member this.CalculateSecondaryCommand = command <| fun () -> graph.UpdateNode("out10")
    member this.Graph = graph
