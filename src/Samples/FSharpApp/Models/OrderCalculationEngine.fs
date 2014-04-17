namespace FSharpApp.Models

open System.Threading
open Arcadia
open FSharpApp.Data


type IOrderCalculationEngine =
    abstract Inventory : Inventory with get, set
    abstract Order : Order with get, set
    abstract OrderResult : OrderResult with get
    abstract AutoCalculate : bool with get, set

module OrderMethods = 
    let getOrderResult(order : Order, inventory : Inventory) = 
        Thread.Sleep 1000
        let price = seq [ for p in inventory.Products -> (p.ID, p.UnitPrice) ] |> dict
        let preTaxAmount = order.Items |> Seq.sumBy(fun i -> price.[i.ProductId] * float i.Units)
        { TotalUnits = order.Items |> Seq.sumBy(fun i -> i.Units)
          PreTaxAmount = preTaxAmount
          TaxAmount = preTaxAmount * order.Tax
          TotalAmount = preTaxAmount * (1. + order.Tax) }

type OrderCalculationEngine(data : IDataService) as this = 
    inherit CalculationEngine()

    // input backing fields
    let inventory = this.Setable(data.LoadInventory(), "Inventory")
    let order = this.Setable(data.LoadOrder(), "Order")

    // output backing fields
    let orderResult = this.Computed((fun() -> OrderMethods.getOrderResult(order.Value, inventory.Value)), "OrderResult")

    member this.Inventory
        with get() = inventory.Value
         and set v = inventory.Value <- v
    member this.Order
        with get() = order.Value
         and set v = order.Value <- v
    member this.OrderResult = orderResult.Value


    interface IOrderCalculationEngine with
        member this.Inventory 
            with get() = inventory.Value
             and set v = inventory.Value <- v
        member this.Order
            with get() = order.Value
             and set v = order.Value <- v
        member this.OrderResult = orderResult.Value
        member this.AutoCalculate 
            with get() = this.Calculation.Automatic
             and set v = this.Calculation.Automatic <- v