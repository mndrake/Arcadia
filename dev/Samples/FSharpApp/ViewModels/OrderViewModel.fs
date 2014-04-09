namespace FSharpApp.ViewModels

open System.ComponentModel
open System.Linq
open Arcadia
open Arcadia.MVVM
open FSharpApp.Models

//type OrderViewModel(ce : IOrderCalculationEngine) as this = 
//    inherit PageViewModel()
//
//    do
//        ce.OrderResult.Changed.Add(fun _ -> this.RaisePropertyChanged "OrderResult")
//
//    override this.Name = "Order"
//
//    member this.Order with get() = ce.Order.Value
//                       and set v = ce.Order.Value <- v
//                                   this.RaisePropertyChanged "Order"
//
//    member this.Items with get() = ce.Order.Value.Items
//                       and set v = ce.Order.Value.Items <- v
//                                   this.RaisePropertyChanged "Items"
//
//    member this.Inventory with get() = ce.Inventory.Value
//                           and set v = ce.Inventory.Value <- v
//                                       this.RaisePropertyChanged "Inventory"
//
//    member this.OrderResult = ce.OrderResult.Value
//
//    member this.AddOrderItemCommand = ActionCommand(fun() -> this.Order.Items.AddNew() |> ignore)