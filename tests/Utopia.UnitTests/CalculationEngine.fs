
#if INTERACTIVE
#r "bin/debug/Utopia.dll"
#r "bin/debug/Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll"
#r "bin/debug/Foq.dll"
#else
namespace Utopia.UnitTests
#endif

open Foq
open Utopia
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type Test_CalculationEngine() =
    [<TestMethod>]        
    member this.``can add input``() =

        let ce = CalculationEngine()
        let input = ce.AddInput(1)
        
        Assert.AreEqual(1, input.Value)

    [<TestMethod>]
    member this.``can add output``() =

        let ce = CalculationEngine()
        let input = ce.AddInput(1)
        let output = ce.AddOutput(input, fun (x:int) -> x)

        Assert.AreEqual(NodeStatus.Dirty, output.Status)

    [<TestMethod>]
    member this.``can evaluate node``() =
        // Arrange
        let ce = CalculationEngine()
        let input = ce.AddInput(1)
        let output = ce.AddOutput(input, fun (x:int) -> x)
        // Act
        CalculationEngine.Evaluate (output)
        Async.RunSynchronously(Async.AwaitEvent(output.Changed), 1000) |> ignore
        // Assert
        Assert.AreEqual(1, output.Value)

    [<TestMethod>]
    member this.``Calculates on Automatic Calculation``() =
        // Arrange
        let ce = CalculationEngine()
        let input1 = ce.AddInput(1)
        let input2 = ce.AddInput(1)
        let output = ce.AddOutput((input1,input2), fun (x,y) -> x+y)
        // Act
        ce.Calculation.Automatic <- true
        Async.RunSynchronously(Async.AwaitEvent(output.Changed), 1000) |> ignore
        // Assert
        Assert.AreEqual(2, output.Value)

    [<TestMethod>]
    member this.``Can Cancel Calculation``() =
        // Arrange
        let ce = CalculationEngine()
        let input = ce.AddInput(1)
        let output = ce.AddOutput(input, (fun (x:int) -> System.Threading.Thread.Sleep 1000; x))
        let triggered = ref false
        output.Cancelled.Add(fun _ -> triggered := true)
        // Act
        CalculationEngine.Evaluate output
        ce.Calculation.Cancel()
        Async.RunSynchronously(Async.AwaitEvent(output.Cancelled), 1000) |> ignore
        // Assert
        Assert.IsTrue(!triggered)

    [<TestMethod>]
    member this.``Output is dirty when changed``() =
        // Arrange
        let ce = CalculationEngine()
        let input = ce.AddInput(1)
        let output = ce.AddOutput(input, (fun (x:int) -> x))
        // Act
        async {
            CalculationEngine.Evaluate output
            let! args = Async.AwaitEvent(output.Changed)
            input.Value <- 2
            let! args = Async.AwaitEvent(output.Changed) 
            do ()} 
        |> Async.RunSynchronously
        // Assert
        Assert.AreEqual(NodeStatus.Dirty, output.Status)
