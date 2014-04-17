namespace FSharpApp.ViewModels

open System.ComponentModel
open System.Linq
open Arcadia
open Arcadia.MVVM
open FSharpApp.Models

type OrderViewModel(ce : IOrderCalculationEngine) = 
    inherit PageViewModel()
    let model = ce

    member this.Model : IOrderCalculationEngine = ce

    override this.Name = "Order"

    member this.AddOrderItemCommand = ActionCommand(fun() -> this.Model.Order.Items.AddNew() |> ignore)
