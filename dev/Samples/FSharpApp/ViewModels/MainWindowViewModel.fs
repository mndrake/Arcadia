namespace FSharpApp.ViewModels

//open System.Collections.ObjectModel
//open Arcadia.MVVM
//open FSharpApp.Data
//open FSharpApp.Models
//
//type MainWindowViewModel() =
//    inherit ViewModelBase()
//
//    let pages = new ObservableCollection<PageViewModel>()
//    let mutable currentPage = new PageViewModel()
//
//    // services
//    let dataService = new DataService()
//
//    // models
//    let orderCalc = OrderCalculationEngine(dataService)
//
//    // page viewmodels
//    let orderVM = new OrderViewModel(orderCalc)
//
//    let addPage page = pages.Add(page :> PageViewModel)
//
//    do
//          orderVM |> addPage
//          currentPage <- pages.[0]
//
//          // set order calculation engine to automatic calculation
//          //orderCalc.Calculation.Automatic <- true
//
//    member val CurrentPage = currentPage
//   
//    member this.Title = "Utopia v0.0"
//    member this.PageViewModels = pages