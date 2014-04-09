namespace Arcadia.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using QuickGraph;
    using Arcadia;

    public delegate INodeVertex VertexConstructor(INode node);

    public class NodeGraph : BidirectionalGraph<INodeVertex, NodeEdge>
    {
        bool _isInitialized;

        private INodeVertex GetVertex(string id)
        {
            return Vertices.First(v => v.Id == id);
        }

        public event EventHandler Initialized;

        protected virtual void OnInitialized(EventArgs e)
        {
            EventHandler handler = Initialized;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public NodeGraph(ICalculationEngine calculationEngine, VertexConstructor vertexConstructor) : base()
        {
            foreach (var n in calculationEngine.Nodes)
            {
                var vertex = vertexConstructor.Invoke(n);
                AddVertex(vertex);

                if (n.Status == NodeStatus.Uninitialized)
                {
                    n.Changed += (sender, e) => 
                    {
                        if (calculationEngine.Nodes.All(node => node.Status != NodeStatus.Uninitialized))
                        {
                            OnInitialized(EventArgs.Empty);                    
                        }
                    };
                }
            }

            foreach (var n in calculationEngine.Nodes)
            {
                foreach (var d in n.GetDependentNodes())
                {                 
                    var edge = new NodeEdge(GetVertex(d.Id), GetVertex(n.Id));
                    AddEdge(edge);
                }
            }
        }

        public void UpdateNode(string Id)
        {
            var vertex = GetVertex(Id);
            vertex.Node.AsyncCalculate();
        }
    }
}
