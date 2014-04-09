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
    let sum = Computed.From(fun() -> i.Value + j.Value)
    let changed = ref false
    while sum.Status <> NodeStatus.Valid
        do Async.RunSynchronously(Async.AwaitEvent(sum.Changed), 2000) |> ignore  
    // Assert
    Assert.AreEqual(5, sum.Value)
    sum.Changed.Add(fun _ -> changed := true)
    // Act
    i.Value <- i.Value + 1
    while (sum.Status <> NodeStatus.Valid) || (!changed = false)
        do Async.RunSynchronously(Async.AwaitEvent(sum.Changed), 2000) |> ignore
    // Assert
    Assert.AreEqual(6, sum.Value)