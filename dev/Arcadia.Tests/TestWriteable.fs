module Arcadia.Eventless.Tests.TestWriteable

open System
open System.Linq
open NUnit.Framework
open Arcadia

[<Test>]
let TestSimpleUnchanged() =
    let w = Setable.From(0)
    w.Changed.Add(fun _ -> failwith "Shouldn't fire Changed event if value is unchanged")
    w.Value <- 0

[<Test>]
let TestSimpleChanged() =
    let w = Setable.From(0)
    let changed = ref false
    w.Changed.Add(fun _ -> changed := true)
    w.Value <- 1
    Assert.IsTrue(!changed)

[<Test>]
let TestCustomEquality() =
    // default comparison uses equals method
    // need to use ResizeArray or System.Collections.Generic.List ... does not work with an F# List
    let changed = ref false
    let w = Setable.From(ResizeArray[5;20;13])
    w.Changed.Add(fun _ -> changed := true)

    w.Value <- ResizeArray[5;20;13] // even though contents are same, still counts as changed

    Assert.IsTrue(!changed)

    changed := false

    // Replace with order-agnostic sequence comparison
    w.EqualityComparer <- 
        fun a b -> a.OrderBy(fun i -> i).SequenceEqual(b.OrderBy(fun i -> i))

    // Same contents in different order, no Changed event
    w.Value <- ResizeArray[5;13;20]
    Assert.IsFalse(!changed)

    // Change one value and it triggers Changed
    w.Value <- ResizeArray[5;3;20]
    Assert.IsTrue(!changed)

[<Test>]
let TestReentrance() =
    let w = Setable.From(0)
    // okay to assign to writeable that just changed as long as it's the same (new) value
    w.Changed.Add(fun _ -> w.Value <- 1)
    w.Value <- w.Value + 1

//[<Test>]
//[<ExpectedException(typeof<RecursiveModificationException>)>]
//let TestRecursion() =
//    let w = Setable.From(0)
//    // not okay to set it to a different value (hense expected exception)
//    w.Changed.Add(fun _ -> w.Value <- w.Value + 1)
//    w.Value <- w.Value + 1