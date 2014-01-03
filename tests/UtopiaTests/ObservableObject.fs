namespace UtopiaTests

open System.ComponentModel
open System.Diagnostics
open Microsoft.VisualStudio.TestTools.UnitTesting    
open Utopia.ViewModel

type TestObject() =
    inherit ObservableObject()

    let mutable a = 0
    let mutable triggeredA = false

    member this.A with get() = a
                   and set v = a <- v
                               this.RaisePropertyChanged("A")

    override this.OnPropertyChanged(args) =
        match args.PropertyName with
        |"A" -> triggeredA <- true
        | _ -> failwith "not a valid property for class"

    member this.TriggeredA = triggeredA

[<TestClass>]
type ObservableCollection() =

    [<TestMethod>]        
    member this.``Can Raise PropertyChanged``() =
        let o = new TestObject()
        o.A <- 2
        Assert.IsTrue(o.TriggeredA)