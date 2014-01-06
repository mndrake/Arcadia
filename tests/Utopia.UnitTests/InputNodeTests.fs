namespace Utopia.UnitTests

open Microsoft.VisualStudio.TestTools.UnitTesting    
open Foq
open Utopia

[<TestClass>]
type InputNodeTests() =
    [<TestMethod>]        
    member this.``Changed Triggered on New Value``() =
    
        // Arrange 

        let triggered = ref false

//        let instance = 
//            { new InputNode<int>() with
//                
//            }
//            Mock<InputNode<int>>()
//                .Create()

//        instance.Changed.Add(fun _ -> triggered := true)
//
//        // Act
//
//        instance.Value <- 1

        // Assert

        Assert.IsTrue(!triggered)