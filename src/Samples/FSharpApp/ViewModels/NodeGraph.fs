namespace FSharpApp.ViewModels

open System
open System.Collections.Generic
open System.Diagnostics
open QuickGraph
open GraphSharp.Controls
open Utopia
open Utopia.ViewModel


type INodeVertex = 
    abstract ID : string
    abstract Node : INode

type NodeEdge(source : INodeVertex, target : INodeVertex) = 
    inherit Edge<INodeVertex>(source, target)
    member this.ID : string = sprintf "{%s} -> {%s}" source.ID target.ID
    override this.ToString() = this.ID

type NodeGraph(ce : ICalculationEngine, vertexConstructor : (INode -> INodeVertex)) as this = 
    inherit BidirectionalGraph<INodeVertex, NodeEdge>()
    let vertices = new Dictionary<string, INodeVertex>()
    
    do 
        for n in ce.Nodes do
            let vertex = vertexConstructor(n)
            vertices.Add(n.ID, vertex)
            this.AddVertex(vertex) |> ignore
        for n in ce.Nodes do
            for d in n.DependentNodes do
                let edge = NodeEdge(vertices.[d.ID], vertices.[n.ID])
                this.AddEdge(edge) |> ignore
    
    member this.CalcEngine = ce
    member this.UpdateNode(id : string) = 
        let vertex = vertices.[id]
        vertex.Node.AsyncCalculate()

type NodeGraphLayout() = 
    inherit GraphLayout<INodeVertex, NodeEdge, NodeGraph>()

type NodeVertexBase(node : INode) as this =
    inherit ViewModelBase()

    do
        node.Changed.Add(fun args -> this.RaisePropertyChanged "Dirty")

    member val Node = node
    member val ID = node.ID
    member val IsInput = node.IsInput

    member this.Dirty with get () = node.Dirty
    override this.ToString() = sprintf "{%s}-{%b}" this.ID this.IsInput
                   
    interface INodeVertex with
        member I.ID = this.ID
        member I.Node = this.Node

