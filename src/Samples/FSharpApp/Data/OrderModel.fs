namespace FSharpApp.Data

open System
open System.ComponentModel
open Arcadia.ViewModel

type Product = 
    { ID : int
      Name : string
      UnitPrice : float }

type Inventory = 
    { Products : Product [] }

type OrderItem() = 
    inherit ObservableObject()
    let mutable productId = 0
    let mutable units = 0
    
    member this.ProductId 
        with get () = productId
        and set v = 
            productId <- v
            this.RaisePropertyChanged "ProductId"
    
    member this.Units 
        with get () = units
        and set v = 
            units <- v
            this.RaisePropertyChanged "Units"

type Order() = 
    inherit ObservableObject()
    let mutable id = 0
    let mutable date = DateTime.Now
    let mutable items = BindingList<OrderItem>()
    let mutable tax = 0.
    
    member this.ID 
        with get () = id
        and set v = 
            id <- v
            this.RaisePropertyChanged "ID"
    
    member this.Date 
        with get () = date
        and set v = 
            date <- v
            this.RaisePropertyChanged "Date"
    
    member this.Items 
        with get () = items
        and set v = 
            items <- v
            items.ListChanged.Add(fun _ -> this.RaisePropertyChanged "Items")
            this.RaisePropertyChanged "Items"
    
    member this.Tax 
        with get () = tax
        and set v = 
            tax <- v
            this.RaisePropertyChanged "Tax"

type OrderResult = 
    { TotalUnits : int
      PreTaxAmount : float
      TaxAmount : float
      TotalAmount : float }