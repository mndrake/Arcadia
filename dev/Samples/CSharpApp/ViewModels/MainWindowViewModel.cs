namespace CSharpApp.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using Arcadia.MVVM;
    using Data;
    using Models;

    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel() : base()
        {
            // services
            var dataService = new DataService();

            // models
            var simpleCalc = new SimpleCalculationEngine();
            var orderCalc = new OrderCalculationEngine(dataService);

            // page viewmodels
            PageViewModels = new ObservableCollection<PageViewModel>();
            PageViewModels.Add(new SimpleGraphViewModel(simpleCalc));
            PageViewModels.Add(new OrderViewModel(orderCalc));
            PageViewModels.Add(new OrderGraphViewModel(orderCalc));

            CurrentPage = PageViewModels.FirstOrDefault();

            // set order calculation engine to automatic calculation
            orderCalc.Calculation.Automatic = true;
        }

        public PageViewModel CurrentPage { get; set; }

        public ObservableCollection<PageViewModel> PageViewModels { get; private set; }

        public string Title {get { return "Utopia v0.0"; } }
    }
}