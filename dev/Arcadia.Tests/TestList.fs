module Arcadia.Tests.TestList
open System
open System.Collections.Generic
open System.Linq
open NUnit.Framework
open Arcadia

[<Test>]
let TestSimpleList() =
    let l = new SetableList<string>()
    let added = new List<int>()
    let removed = new List<int>()
    let updated = new List<int>()
    let cleared = ref 0

    let on_added = Action<int>(fun arg -> added.Add(arg))
    let on_removed = Action<int>(fun arg -> removed.Add(arg))
    let on_updated = Action<int>(fun arg -> updated.Add(arg))
    let on_cleared = Action(fun () -> incr cleared)

    let on_added_handler = Handler(fun sender (action:Action<int>) -> ())
    
    l.Added.Add(fun arg -> added.Add(arg.Value))
    l.Removed.Add(fun arg -> removed.Add(arg.Value))
    l.Updated.Add(fun arg -> updated.Add(arg.Value))
    l.Cleared.Add(fun arg -> incr cleared)

    l.Add("a")
    l.Add("b")
    l.Add("c")
    l.Add("d")

    Assert.AreEqual(4, added.Count)
    Assert.AreEqual(0, added.[0])
    Assert.AreEqual(1, added.[1])
    Assert.AreEqual(2, added.[2])
    Assert.AreEqual(3, added.[3])

    l.RemoveAt(1)

    Assert.AreEqual(4, added.Count)
    Assert.AreEqual(1, removed.Count)
    Assert.AreEqual(1, removed.[0])

    l.Insert(1, "x")

    Assert.AreEqual(5, added.Count)
    Assert.AreEqual(1, added.[4])
    Assert.AreEqual(1, removed.Count)

    l.[2] <- "y"

    Assert.AreEqual(5, added.Count)
    Assert.AreEqual(1, removed.Count)
    Assert.AreEqual(2, updated.[0])

    Assert.AreEqual("a", l.[0])
    Assert.AreEqual("x", l.[1])
    Assert.AreEqual("y", l.[2])
    Assert.AreEqual("d", l.[3])

[<Test>]
let TestReentrance() =
    let l = new SetableList<int>(ResizeArray [1;2;3;4;5;6])
    l.Changed.Add(fun arg ->         
        for i = 0 to l.Count-1 do
            l.[i] <- 10)

    l.[3] <- 10

    for t in l do
        Assert.AreEqual(10, t)
