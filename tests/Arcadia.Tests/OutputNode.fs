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
open System.Threading

[<Test>]
let ``Changed Event Triggered on Input Changed``() = 
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
    Async.RunSynchronously(
        async {
            event.Trigger(input, ChangedEventArgs(NodeStatus.Valid))
            let! args = Async.AwaitEvent(output.Changed)
            do () },
        2000)
    // Assert
    Assert.IsTrue(!triggered)

[<Test>]
let ``Can Process Node Function``() = 
    // Arrange
    let cts = new CancellationTokenSource()
    let calc = Mock<ICalculationHandler>()
                .Setup(fun x -> <@ x.Automatic @>).Returns(true)
                .Setup(fun x -> <@ x.CancellationToken @>).Returns(cts.Token)
                .Create()
    let input1 = 
        Mock<INode>().Setup(fun x -> <@ x.Evaluate() @>).Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let input2 = 
        Mock<INode>().Setup(fun x -> <@ x.Evaluate() @>).Returns(async { return (NodeStatus.Valid, box 1) }).Create()
    let output = OutputNode(calc, (input1, input2), fun (x, y) -> x + y)
    // Act
    Async.RunSynchronously(
        async {
            let! (status, value) = output.Evaluate()
            printfn "%A, %A" status value
            do () },
        2000)
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
