namespace SampleApp.ViewModel

open System.Linq
open Utopia
open Utopia.ViewModel

type GraphViewModel() = 
    inherit PageViewModel()
    let graph = new CustomGraph()
    member this.CalculationTypes = [ "Automatic"; "Manual" ].ToList()
    
    member this.CalculationType 
        with get () = 
            match graph.CalculationMode with
            | CalculationMode.Automatic -> "Automatic"
            | CalculationMode.Manual -> "Manual"
        and set v = 
            match v with
            | "Automatic" -> graph.CalculationMode <- CalculationMode.Automatic
            | "Manual" -> graph.CalculationMode <- CalculationMode.Manual
            | _ -> failwith "not a valid calculation type"
            this.OnPropertyChanged "CalculationType"
    
    //member this.CalcEngine = graph
    member this.LayoutAlgorithmType = "EfficientSugiyama"
    override this.Name = "Graph"
    member this.CalculateFullCommand = 
        Utils.command <| fun () -> Async.Start <| async { let! result = graph.EvalNode("out9")
                                                          this.OnPropertyChanged "Graph" }
    member this.CancelCalculateCommand = Utils.command <| fun () -> Async.CancelDefaultToken()
    member this.CalculatePartialCommand = 
        Utils.command(fun () -> async { let! result = graph.EvalNode("out4")
                                        this.OnPropertyChanged "Graph" } |> Async.Start)
    member this.CalculateSecondaryCommand = 
        Utils.command(fun () -> async { let! result = graph.EvalNode("out10")
                                        this.OnPropertyChanged "Graph" } |> Async.Start)
    member this.Graph = graph