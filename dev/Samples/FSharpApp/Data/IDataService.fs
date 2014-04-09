namespace FSharpApp.Data

type IDataService =
    abstract LoadInventory : unit -> Inventory
    abstract LoadOrder : unit -> Order