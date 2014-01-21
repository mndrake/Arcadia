module Arcadia.Tests.OutputNode

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
let ``Changed Triggered on Input Changed``() = 
    // Arrange
    let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
    let event = Event<_, _>()
    let input = 
        Mock<INode>().SetupEvent(fun x -> <@ x.Changed @>).Publishes(event.Publish).Setup(fun x -> <@ x.Evaluate() @>)
            .Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let output = OutputNode(calc, input, fun (x : int) -> x)
    let triggered = ref false
    output.Changed.Add(fun _ -> triggered := true)
    // Act
    event.Trigger(input, System.EventArgs.Empty)
    // since output nodes process asynchronously wait for changed event
    Async.RunSynchronously(Async.AwaitEvent(output.Changed), 1000) |> ignore
    // Assert
    Assert.IsTrue(!triggered)

[<Test>]
let ``Can Process Node Function``() = 
    // Arrange
    let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
    let input1 = 
        Mock<INode>().Setup(fun x -> <@ x.Evaluate() @>).Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let input2 = 
        Mock<INode>().Setup(fun x -> <@ x.Evaluate() @>).Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let output = OutputNode(calc, (input1, input2), fun (x, y) -> x + y)
    // Act
    output.AsyncCalculate()
    // since output nodes process asynchronously wait for changed event
    Async.RunSynchronously(Async.AwaitEvent(output.Changed), 1000) |> ignore
    // Assert
    Assert.AreEqual(2, output.Value)

[<Test>]
let ``Fail on Setting Value of Output``() = 
    // Arrange
    let calc = Mock<ICalculationHandler>().Setup(fun x -> <@ x.Automatic @>).Returns(true).Create()
    let input = 
        Mock<INode>().Setup(fun x -> <@ x.Evaluate() @>).Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let output = OutputNode(calc, input, fun (x : int) -> x)
    try 
        output.Value <- 4
        false
    with _ -> true
    |> Assert.IsTrue
