namespace FSharpApp.Models

open System.Threading
open Arcadia
open FSharpApp.Data

namespace FSharpApp.Models

open System.Threading
open Arcadia
open FSharpApp.Data

type IOrderCalculationEngine =
    abstract Inventory : INode<Inventory>
    abstract Order : INode<Order>
    abstract OrderResult : INode<OrderResult>
    abstract AutoCalculate : bool with get,set

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

    // input nodes
    member this.Inventory = inventory
    member this.Order = order

    // output nodes
    member this.OrderResult = orderResult

    interface IOrderCalculationEngine with
        member I.Inventory = this.Inventory.ToINode()
        member I.Order = this.Order.ToINode()
        member I.OrderResult = this.OrderResult.ToINode()
        member I.AutoCalculate with get() = this.Calculation.Automatic
                                and set v = this.Calculation.Automatic <- v