namespace CSharpApp.ViewModel
{
    using System;
    using System.Windows.Input;
    using Utopia;
    using Utopia.ViewModel;

    public class GraphViewModel : PageViewModel
    {
        ICalculationEngine _calculationEngine;

        public bool[] BooleanTypes { get { return new bool[] { true, false }; } }

        public bool AutoCalculate
        {
            get { return _calculationEngine.Calculation.Automatic; }
            set { _calculationEngine.Calculation.Automatic = value; }
        }

        public GraphViewModel()
        {
            new GraphViewModel(new CalculationEngine());
        }

        public GraphViewModel(ICalculationEngine calculationEngine)
            : base()
        {
            _calculationEngine = calculationEngine;

            Graph = new CustomGraph(_calculationEngine);

            CalculateFullCommand = new ActionCommand(() => Graph.Update("out9"));
            CancelCalculateCommand = new ActionCommand(() => _calculationEngine.Calculation.Cancel());
            CalculatePartialCommand = new ActionCommand(() => Graph.Update("out4"));
            CalculateSecondaryCommand = new ActionCommand(() => Graph.Update("out10"));
        }

        public string LayoutAlgorithmType { get { return "EfficientSugiyama"; } }

        public override string Name { get { return "Graph"; } }

        public CustomGraph Graph { get; private set; }
       
        public ICommand CalculateFullCommand { get; private set; }
        public ICommand CancelCalculateCommand { get; private set; }
        public ICommand CalculatePartialCommand { get; private set; }
        public ICommand CalculateSecondaryCommand { get; private set; }
    }
}