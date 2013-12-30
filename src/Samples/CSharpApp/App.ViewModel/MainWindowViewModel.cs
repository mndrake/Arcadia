namespace SampleApp.ViewModelCS
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using Utopia.ViewModel;
    using SampleApp.Model;

    public class MainWindowViewModel : ViewModelBase
    {
        PageViewModel _currentPage;

        public PageViewModel CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                OnPropertyChanged("CurrentPage");
            }
        }

        public ObservableCollection<PageViewModel> PageViewModels { get; private set; }

        public MainWindowViewModel() : base()
        {
            var calcEngine = new CalcEngineModel();

            PageViewModels = new ObservableCollection<PageViewModel>();
            PageViewModels.Add(new GraphViewModel(calcEngine));

            CurrentPage = PageViewModels.FirstOrDefault();
        }

        public string Title {get { return "Calc FrameWork v0.1"; } }
    }
}