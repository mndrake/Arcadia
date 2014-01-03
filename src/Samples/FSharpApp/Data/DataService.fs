namespace FSharpApp.Data

open System
open System.ComponentModel

type DataService() as this =

    member this.LoadInventory() = 
        { Products = 
              [| { ID = 0
                   Name = "<Select Item>"
                   UnitPrice = 0. }
                 { ID = 1
                   Name = "Dharamsala Tea"
                   UnitPrice = 18. }
                 { ID = 2
                   Name = "Tibetan Barley Beer"
                   UnitPrice = 19. }
                 { ID = 3
                   Name = "Aniseed Syrup"
                   UnitPrice = 10. }
                 { ID = 4
                   Name = "Chef Anton's Cajun Seasoning"
                   UnitPrice = 22. }
                 { ID = 5
                   Name = "Chef Anton's Gumbo Mix"
                   UnitPrice = 21.35 } |] }

    member this.LoadOrder() =    
        let items = new BindingList<OrderItem>()
        items.Add(new OrderItem(ProductId = 1, Units = 10))
        items.Add(new OrderItem(ProductId = 3, Units = 5))
        new Order(ID = 12345, Date = DateTime.Parse("01-01-2014"), Tax = 0.07, Items = items)

    interface IDataService with
        member I.LoadInventory() = this.LoadInventory()
        member I.LoadOrder() = this.LoadOrder()