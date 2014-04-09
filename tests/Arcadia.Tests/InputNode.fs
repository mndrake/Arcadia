module Arcadia.Tests.InputNode

#if INTERACTIVE
#r "../../bin/Arcadia.dll"
#r "../../packages/Foq.1.4/Lib/net40/Foq.dll"
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#endif

open NUnit.Framework
open Foq
open Arcadia
open System.ComponentModel

[<Test>]
let ``Changed Triggered on New Value``() = 
    // Arrange 
    let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
    let input = InputNode(calc,"",0)
    let triggered = ref false
    input.Changed.Add(fun _ -> triggered := true)
    // Act
    input.Value <- 1
    // Assert
    Assert.IsTrue(!triggered)

[<Test>]
let ``Changed Triggered on INotifyPropertyChanged``() = 
    // Arrange
    let event = Event<_, _>()
    let mock = 
        Mock<INotifyPropertyChanged>().SetupEvent(fun x -> <@ x.PropertyChanged @>).Publishes(event.Publish)
            .Create()
    let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
    let input = InputNode(calc,"",mock)
    let triggered = ref false
    input.Changed.Add(fun _ -> triggered := true)
    // Act
    event.Trigger(mock, PropertyChangedEventArgs("PropertyName"))
    // Assert
    Assert.IsTrue(!triggered)