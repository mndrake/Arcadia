namespace SampleApp.ViewModel

open GraphSharp.Controls
open QuickGraph
open SampleApp.Model
open System
open System.Collections.Generic
open System.Diagnostics
open Utopia
open Utopia.ViewModel

[<DebuggerDisplay("{ID}-{IsInput}")>]
type CustomVertex(node : INode) as this = 
    inherit ViewModelBase()
    
    let tryParseInt(v : string) = 
        try 
            Convert.ToInt32(v) |> Some
        with _ -> None
    
    do 
        node.Changed.Add(fun args -> 
            this.OnPropertyChanged "Value"
            this.OnPropertyChanged "Dirty")
    
    member this.IsValidValue() = 
        let isInt(v : string) = 
            try 
                Convert.ToInt32(v) |> ignore
                true
            with _ -> false
        if String.IsNullOrEmpty(this.Value) || this.Value = null then "missing value"
        elif tryParseInt(this.Value).IsNone then "not a valid number"
        else null
    
    override this.ValidatedProperties = [| "Value" |]
    
    override this.GetValidationError(propertyName) = 
        match propertyName with
        | "Value" -> this.IsValidValue()
        | _ -> 
            Debug.Fail("Unexpected property being validated on Vertex: " + node.ID)
            Unchecked.defaultof<string>
    
    member val Node = node
    member val ID = node.ID
    member val IsInput = (node.DependentNodes.Length = 0)
    
    member this.Value 
        with get () = string node.UntypedValue
        and set v = 
            match tryParseInt(v) with
            | Some x -> node.UntypedValue <- box x
            | None -> ()
    
    member this.Dirty with get () = node.Dirty
    override this.ToString() = sprintf "{%s}-{%b}" this.ID this.IsInput

[<DebuggerDisplay("{source.ID} -> {target.ID}")>]
type CustomEdge(source, target) = 
    inherit Edge<CustomVertex>(source, target)
    member this.ID : string = sprintf "{%s} -> {%s}" source.ID target.ID
    override this.ToString() = this.ID

type CustomGraph(ce : ICalculationEngine) as this = 
    inherit BidirectionalGraph<CustomVertex, CustomEdge>()
    let vertices = new Dictionary<string, CustomVertex>()
    
    do 
        for n in ce.Nodes do
            let vertex = new CustomVertex(n)
            vertices.Add(n.ID, vertex)
            this.AddVertex(vertex) |> ignore
        for n in ce.Nodes do
            for d in n.DependentNodes do
                let edge = CustomEdge(vertices.[d.ID], vertices.[n.ID])
                this.AddEdge(edge) |> ignore
    
    member this.CalcEngine = ce
    member this.UpdateNode(id : string) = 
        let vertex = vertices.[id]
        vertex.Node.AsyncCalculate()

type CustomGraphLayout() = 
    inherit GraphLayout<CustomVertex, CustomEdge, CustomGraph>()
