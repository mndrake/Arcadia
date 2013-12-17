namespace SampleApp.ViewModel

open System.Collections.ObjectModel
open Utopia.ViewModel

type MainWindowViewModel() =
    inherit ViewModelBase()
    let mutable currentpage:PageViewModel = new PageViewModel()
    let pages = new ObservableCollection<PageViewModel>()

    // viewmodels of app
    let graphVM = new GraphViewModel()

    let addPage page = pages.Add(page :> PageViewModel)

    do
          graphVM |> addPage
          currentpage <- pages.[0]
   
    member this.Title = "Calc FrameWork v0.1"
    member this.PageViewModels = pages