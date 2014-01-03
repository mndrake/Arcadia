namespace CSharpApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using GraphSharp.Controls;
    using QuickGraph;
    using Utopia;
    using Utopia.ViewModel;

    public interface INodeVertex
    {
        string ID { get; }
        INode Node { get; }
    }

    public class NodeEdge : Edge<INodeVertex>
    {
        public string ID { get; private set; }

        public NodeEdge(INodeVertex source, INodeVertex target)
            : base(source, target)
        {
            ID = string.Format("{0} -> {1}", source.ID, target.ID);

        }

        public override string ToString()
        {
            return ID;
        }
    }

    public class NodeGraph : BidirectionalGraph<INodeVertex, NodeEdge>
    {
        IDictionary<string, INodeVertex> _vertices;

        public NodeGraph(ICalculationEngine calculationEngine, Func<INode, INodeVertex> vertexConstructor)
            : base()
        {
            _vertices = new Dictionary<string, INodeVertex>();

            foreach (var node in calculationEngine.Nodes)
            {
                var vertex = vertexConstructor(node);
                _vertices.Add(node.ID, vertex);
                AddVertex(vertex);
            }

            foreach (var node in calculationEngine.Nodes)
            {
                foreach (var dependentNode in node.DependentNodes)
                {
                    var edge = new NodeEdge(_vertices[dependentNode.ID], _vertices[node.ID]);
                    AddEdge(edge);
                }
            }
        }

        public void UpdateNode(string id)
        {
            var vertex = _vertices[id];
            vertex.Node.AsyncCalculate();
        }

    }

    public class NodeGraphLayout : GraphLayout<INodeVertex, NodeEdge, NodeGraph>
    {
        public NodeGraphLayout() : base() { }
    }

    public class NodeVertexBase : ViewModelBase, INodeVertex
    {
        public NodeVertexBase(INode node)
            : base()
        {
            Node = node;

            Node.Changed += new ChangedEventHandler((sender, args) => RaisePropertyChanged("Dirty"));
        }

        public INode Node { get; private set; }

        public string ID { get { return Node.ID; } }

        public bool IsInput { get { return Node.IsInput; } }

        public bool Dirty { get { return Node.Dirty; } }

        public override string ToString() { return string.Format("{0}-{1}", ID, IsInput); }
    }
}