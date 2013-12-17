namespace SampleApp.ViewModel

open System.Collections.Generic
open System.Diagnostics
open GraphSharp.Controls
open QuickGraph
open Utopia
open Utopia.ViewModel
open SampleApp.Model

[<DebuggerDisplay("{ID}-{IsInput}")>]
type CustomVertex(node : INode) as this = 
    inherit ViewModelBase()
    let mutable dirty = node.Dirty
    do 
        node.Changed.Add(fun () -> 
            this.OnPropertyChanged "Value"
            this.OnPropertyChanged "Dirty")    
    member val Node = node
    member val ID = node.ID
    member val IsInput = (node.DependentNodes.Length = 0)    
    member this.Value 
        with get () = node.CurrentValue.ToString()
        and set (v:string) = 
            node.CurrentValue <- box <| System.Convert.ToInt32(v)
            this.OnPropertyChanged "Value"
    member this.Dirty with get () = node.Dirty
    override this.ToString() = sprintf "{%s}-{%b}" this.ID this.IsInput

[<DebuggerDisplay("{source.ID} -> {target.ID}")>]
type CustomEdge(source, target) = 
    inherit Edge<CustomVertex>(source, target)
    member this.ID : string = sprintf "{%s} -> {%s}" source.ID target.ID
    override this.ToString() = this.ID

type CustomGraph() as this = 
    inherit BidirectionalGraph<CustomVertex, CustomEdge>()
    let ce = CalcEngine
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

    member this.CalculationMode with get() = ce.Calculation
                                 and set v = ce.Calculation <- v

    member this.EvalNode (id : string) = 
        let vertex = vertices.[id]
        vertex.Node.Eval

type CustomGraphLayout() = 
    inherit GraphLayout<CustomVertex, CustomEdge, CustomGraph>()