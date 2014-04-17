namespace FSharpApp.ViewModels

open System.Collections.ObjectModel
open Arcadia.MVVM
open FSharpApp.Data
open FSharpApp.Models

type MainWindowViewModel() =
    inherit ViewModelBase()

    let pages = new ObservableCollection<PageViewModel>()
    let mutable currentPage = new PageViewModel()

    // services
    let dataService = new DataService()

    // models
    let simpleCalc = new SimpleCalculationEngine()
    let orderCalc = OrderCalculationEngine(dataService)

    // page viewmodels
    let simpleGraphVM = new SimpleGraphViewModel(simpleCalc)
    let orderVM = new OrderViewModel(orderCalc)
    let orderGraphVM = new OrderGraphViewModel(orderCalc)

    let addPage page = pages.Add(page :> PageViewModel)

    do
          simpleGraphVM |> addPage
          orderVM |> addPage
          orderGraphVM |> addPage
          currentPage <- pages.[0]

          // set order calculation engine to automatic calculation
          orderCalc.Calculation.Automatic <- true

    member val CurrentPage = currentPage
   
    member this.Title = "Arcadia v0.2"
    member this.PageViewModels = pages