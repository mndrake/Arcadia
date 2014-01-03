namespace CSharpApp.ViewModels
{
    using System;
    using Utopia;
    using Utopia.ViewModel;

    public class OrderGraphVertex : NodeVertexBase
    {
        public OrderGraphVertex(INode node) : base(node) { }
    }

    public class OrderGraphViewModel : PageViewModel
    {
        ICalculationEngine _calculationEngine;
        NodeGraph _graph;

        public OrderGraphViewModel(ICalculationEngine calculationEngine) : base()
        {
            _calculationEngine = calculationEngine;
            Func<INode, INodeVertex> vertexConstructor = node => (INodeVertex)new OrderGraphVertex(node);
            _graph = new NodeGraph(calculationEngine, vertexConstructor);
        }

        public bool AutoCalculate
        {
            get { return _calculationEngine.Calculation.Automatic; }
            set { _calculationEngine.Calculation.Automatic = value; }
        }

        public bool[] BooleanTypes { get { return new bool[] { true, false }; } }

        public NodeGraph Graph { get { return _graph; } }

        public string LayoutAlgorithmType { get { return "EfficientSugiyama"; } }

        public override string Name { get { return "OrderGraph"; } }
    }
}