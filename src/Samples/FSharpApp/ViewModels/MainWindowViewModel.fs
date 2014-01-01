namespace FSharpApp.ViewModels

open System.Collections.ObjectModel
open Utopia.ViewModel
open FSharpApp.Models

type MainWindowViewModel() =
    inherit ViewModelBase()
    let mutable currentpage:PageViewModel = new PageViewModel()
    let pages = new ObservableCollection<PageViewModel>()

    let ce = CalculationEngineModel()

    // viewmodels of app
    let graphVM = new GraphViewModel(ce)

    let addPage page = pages.Add(page :> PageViewModel)

    do
          graphVM |> addPage
          currentpage <- pages.[0]
   
    member this.Title = "Calc FrameWork v0.1"
    member this.PageViewModels = pages