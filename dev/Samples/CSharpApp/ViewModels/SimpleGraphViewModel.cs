namespace CSharpApp.ViewModels
{
    using System;
    using System.Linq;
    using System.Windows.Input;
    using Arcadia;
    using Arcadia.MVVM;
    using Arcadia.Graph;

    public class SimpleGraphVertex : NodeVertexBase
    {
        public SimpleGraphVertex(INode node)
            : base(node)
        {
            Node.Changed += new ChangedEventHandler((sender, args) => RaisePropertyChanged("Value"));
        }

        public int Value
        {
            get { return (int)Node.UntypedValue; }
            set { Node.UntypedValue = value; }
        }
    }

    public class SimpleGraphViewModel : PageViewModel
    {
        ICalculationEngine _calculationEngine;
        bool _isInitialized;

        public bool[] BooleanTypes { get { return new bool[] { true, false }; } }

        public bool AutoCalculate
        {
            get { return _calculationEngine.Calculation.Automatic; }
            set { _calculationEngine.Calculation.Automatic = value; }
        }

        public SimpleGraphViewModel()
        {
            new SimpleGraphViewModel(new CalculationEngine());
        }

        public SimpleGraphViewModel(ICalculationEngine calculationEngine) : base()
        {
            _calculationEngine = calculationEngine;

            Graph = new NodeGraph(_calculationEngine, new VertexConstructor(node => (INodeVertex)new SimpleGraphVertex(node)));

            Graph.Initialized += Graph_Initialized;

            UpdateGraphCommand = new ActionCommand(() => UpdateGraph());
            CalculateFullCommand = new ActionCommand(() => Graph.UpdateNode("out9"));
            CancelCalculateCommand = new ActionCommand(() => _calculationEngine.Calculation.Cancel());
            CalculatePartialCommand = new ActionCommand(() => Graph.UpdateNode("out4"));
            CalculateSecondaryCommand = new ActionCommand(() => Graph.UpdateNode("out10"));
    
        }

        void Graph_Initialized(object sender, EventArgs e)
        {
            UpdateGraph();
        }

        private void UpdateGraph()
        {
            Graph = new NodeGraph(_calculationEngine, new VertexConstructor(node => (INodeVertex)new SimpleGraphVertex(node)));
            RaisePropertyChanged("Graph");
        }

        public string LayoutAlgorithmType { get { return "EfficientSugiyama"; } }

        public override string Name { get { return "SimpleGraph"; } }

        public NodeGraph Graph { get; private set; }
        public ICommand UpdateGraphCommand { get; private set; }
        public ICommand CalculateFullCommand { get; private set; }
        public ICommand CancelCalculateCommand { get; private set; }
        public ICommand CalculatePartialCommand { get; private set; }
        public ICommand CalculateSecondaryCommand { get; private set; }
    }
}