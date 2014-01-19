
#if INTERACTIVE
#r "bin/debug/Utopia.dll"
#r "bin/debug/Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll"
#r "bin/debug/Foq.dll"
#else
namespace Utopia.UnitTests
#endif


open Microsoft.VisualStudio.TestTools.UnitTesting
open Foq
open Utopia
open System.ComponentModel

[<TestClass>]
type Test_InputNode() = 
    
    [<TestMethod>]
    member this.``Changed Triggered on New Value``() = 
        // Arrange 
        let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
        let input = InputNode(calc, 0)
        let triggered = ref false
        input.Changed.Add(fun _ -> triggered := true)
        // Act
        input.Value <- 1
        // Assert
        Assert.IsTrue(!triggered)
    
    [<TestMethod>]
    member this.``Changed Triggered on INotifyPropertyChanged``() = 
        // Arrange
        let event = Event<_, _>()
        let mock = 
            Mock<INotifyPropertyChanged>().SetupEvent(fun x -> <@ x.PropertyChanged @>).Publishes(event.Publish)
                .Create()
        let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
        let input = InputNode(calc, mock)
        let triggered = ref false
        input.Changed.Add(fun _ -> triggered := true)
        // Act
        event.Trigger(mock, PropertyChangedEventArgs("PropertyName"))
        // Assert
        Assert.IsTrue(!triggered)

    [<TestMethod>]
    member this.``Input Node is Valid``() =
        let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
        let input = InputNode(calc, 2)
        Assert.AreEqual(NodeStatus.Valid, input.Status)