#if INTERACTIVE
#r "bin/Debug/NUnit.Framework.dll"
#r "bin/Debug/Arcadia.dll"
#else
module Arcadia.Tests.TestComputed
#endif

open NUnit.Framework
open Arcadia

[<Test>]
let TestSimple() = 
    // Arrange 
    let i = Setable.From(2)
    let j = Setable.From(3)
    let calc = CalculationHandler()
    //calc.Automatic <- false
    let sum = Computed.From(fun() -> i.Value + j.Value)
    while (Async.AwaitEvent(sum.Changed) |> Async.RunSynchronously).Status <> NodeStatus.Valid do ()
    // Assert
    Assert.AreEqual(5, sum.Value)

    // Act
    i.Value <- i.Value + 1
    while (Async.AwaitEvent(sum.Changed) |> Async.RunSynchronously).Status <> NodeStatus.Valid do ()
    // Assert
    Assert.AreEqual(6, sum.Value)