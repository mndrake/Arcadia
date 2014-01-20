namespace UtopiaTests

open Foq
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.ComponentModel
open System.Diagnostics
open Arcadia.ViewModel

[<TestClass>]
type ObservableCollection() = 

    [<TestMethod>]
    member this.``Can Raise PropertyChanged``() = 
        // Arrange
        let triggered = ref false

        let instance = 
            { new ObservableObject() with
                    member this.OnPropertyChanged(name) = 
                        match name with
                        | "A" -> triggered := true
                        | _ -> () }
        
        // Act
        instance.RaisePropertyChanged "A"
        
        // Assert
        Assert.IsTrue(!triggered)

    [<TestMethod>]
    member this.``Fails On Incorrect PropertyChanged``() =
        // Arrange
        let instance = 
            { new ObservableObject() with
                    member this.OnPropertyChanged(name) = 
                        match name with
                        | "A" -> ()
                        | _ -> failwith "not a valid property" }
        
        // Act
        let failed =
            try
                instance.RaisePropertyChanged "B"
                false
            with
            |_ -> true
        
        // Assert
        Assert.IsTrue(failed)        

