namespace Utopia.Graph
{
    using GraphSharp.Controls;

    public class NodeGraphLayout : GraphLayout<INodeVertex, NodeEdge, NodeGraph>
    {
        public NodeGraphLayout() : base()
        {
        }
    }
}


            //<graph:NodeGraphLayout x:Name="graphLayout" Margin="10"
            //            Graph="{Binding Graph}"
            //            LayoutAlgorithmType="{Binding Path=LayoutAlgorithmType, Mode=OneWay}"
            //            OverlapRemovalAlgorithmType="FSA"
            //            HighlightAlgorithmType="Simple" />