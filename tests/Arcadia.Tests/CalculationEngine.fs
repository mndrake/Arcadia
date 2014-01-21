module Arcadia.Tests.CalculationEngine

#if INTERACTIVE
#r "../../bin/Arcadia.dll"
#r "../../packages/Foq.1.4/Lib/net40/Foq.dll"
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#endif

open NUnit.Framework
open Foq
open Arcadia

[<Test>]        
let ``can add input``() =
    let ce = CalculationEngine()
    let input = ce.AddInput(1)        
    Assert.AreEqual(1, input.Value)

[<Test>]
let ``can add output``() =
    let ce = CalculationEngine()
    let input = ce.AddInput(1)
    let output = ce.AddOutput(input, fun (x:int) -> x)
    Assert.AreEqual(NodeStatus.Dirty, output.Status)

[<Test>]
let ``can evaluate node``() =
    // Arrange
    let ce = CalculationEngine()
    let input = ce.AddInput(1)
    let output = ce.AddOutput(input, fun (x:int) -> x)
    // Act
    Async.RunSynchronously(
        async {
            CalculationEngine.Evaluate (output)
            let! args = Async.AwaitEvent(output.Changed)
            do () },
        2000)
    // Assert
    Assert.AreEqual(1, output.Value)

[<Test>]
let ``Calculates on Automatic Calculation``() =
    // Arrange
    let ce = CalculationEngine()
    let input1 = ce.AddInput(1)
    let input2 = ce.AddInput(1)
    let output = ce.AddOutput((input1,input2), fun (x,y) -> x+y)
    // Act
    Async.RunSynchronously(
        async {
            ce.Calculation.Automatic <- true
            let! args = Async.AwaitEvent(output.Changed)
            do () },
        2000)
    // Assert
    Assert.AreEqual(2, output.Value)

[<Test>]
let ``Can Cancel Calculation``() =
    // Arrange
    let ce = CalculationEngine()
    let input = ce.AddInput(1)
    let output = ce.AddOutput(input, (fun (x:int) -> System.Threading.Thread.Sleep 1000; x))
    let triggered = ref false
    output.Cancelled.Add(fun _ -> triggered := true)
    // Act
    Async.RunSynchronously(
        async {
            CalculationEngine.Evaluate output
            ce.Calculation.Cancel()
            let! args = Async.AwaitEvent(output.Cancelled)
            do ()},
        2000)
    // Assert
    Assert.IsTrue(!triggered)

[<Test>]
let ``Output is dirty when changed``() =
    // Arrange
    let ce = CalculationEngine()
    let input = ce.AddInput(1)
    let output = ce.AddOutput(input, (fun (x:int) -> x))
    // Act
    Async.RunSynchronously(
        async {
            CalculationEngine.Evaluate output
            let! args = Async.AwaitEvent(output.Changed)
            input.Value <- 2
            let! args = Async.AwaitEvent(output.Changed) 
            do ()},
        2000)
    // Assert
    Assert.AreEqual(NodeStatus.Dirty, output.Status)
