namespace Arcadia.Graph
{
    using QuickGraph;
    
    public class NodeEdge : Edge<INodeVertex>
    {
        public NodeEdge(INodeVertex source, INodeVertex target) : base(source, target)
        {
        }

        public string Id { get { return string.Format("{0} -> {1}", Source.Id, Target.Id); } }

        public override string ToString()
        {
            return this.Id;
        }
    }
}