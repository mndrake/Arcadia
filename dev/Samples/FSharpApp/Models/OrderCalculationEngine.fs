namespace FSharpApp.Models

open System.Threading
open Arcadia.Eventless
//open Arcadia.FSharp
open FSharpApp.Data

type IOrderCalculationEngine =
    abstract Inventory : ISetable<Inventory>
    abstract Order : ISetable<Order>
    abstract OrderResult : IGetable<OrderResult>

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
    
    // input backing fields
    let inventory = Setable.From(data.LoadInventory())
    let order = Setable.From(data.LoadOrder())

    // output backing fields
    let orderResult = Computed.From(fun () -> OrderMethods.getOrderResult(order.Value,inventory.Value))
        
    // input nodes
    member this.Inventory = inventory
    member this.Order = order

    // output nodes
    member this.OrderResult = orderResult

    interface IOrderCalculationEngine with
        member I.Inventory = this.Inventory :> ISetable<_>
        member I.Order = this.Order :> ISetable<_>
        member I.OrderResult = this.OrderResult :> IGetable<_>