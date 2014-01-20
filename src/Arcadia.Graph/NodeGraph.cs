namespace Arcadia.Graph
{
using System.Collections.Generic;
    using System.Linq;
using QuickGraph;
using Arcadia;

    public delegate INodeVertex VertexConstructor(INode node);

    public class NodeGraph : BidirectionalGraph<INodeVertex, NodeEdge>
    {
        private INodeVertex GetVertex(string id)
        {
            return Vertices.First(v => v.Id == id);
        }

        public NodeGraph(ICalculationEngine calculationEngine, VertexConstructor vertexConstructor) : base()
        {
            foreach (var n in calculationEngine.Nodes)
            {
                var vertex = vertexConstructor.Invoke(n);
                AddVertex(vertex);
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
