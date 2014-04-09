#r "bin/Debug/Arcadia.dll"

open System.Threading
open Arcadia
open Arcadia.FSharp

// async functions that are used to calculate output nodes
let add2 x1 x2 = 
        Thread.Sleep 250
        x1 + x2

let add3 x1 x2 x3 = 
        Thread.Sleep 500
        x1 + x2 + x3

let add4 x1 x2 x3 x4 = 
        Thread.Sleep 750
        x1 + x2 + x3 + x4

let printUpdate =
    let agent =
        MailboxProcessor<string * NodeStatus>.Start(fun inbox -> 
            let rec loop() = 
                async { 
                    let! (nodeId,status) = inbox.Receive()
                    printfn "Id: %s status: %A" nodeId status
                    return! loop()
                }
            loop())
    let post nodeId nodeStatus = agent.Post(nodeId, nodeStatus)
    post

let engine =
    let e = CalculationEngine()

    let inline set v =
        let n = e.Setable v
        n.Changed.Add(fun arg -> printUpdate n.Id arg.Status)
        n

    let inline calc f =
        let n = e.Computed(fun() -> f())
        n.Changed.Add(fun arg -> printUpdate n.Id arg.Status)
        n        

    let in0 = set 1
    let in1 = set 1
    let in2 = set 1
    let out0 = calc (fun () -> !!in0 + !!in1)
    let out1 = calc (fun () -> !!in1 + !!in2)
    let out2 = calc (fun () -> !!out0 + !!out1)
    let out3 = calc (fun () -> !!in0 + !!out2)
    e

engine.Node<int>("out2")



let changed = Event<int>()
let Changed = changed.Publish

//let initialized (e:IEvent<int>) =
//    let evt = e |> Event.filter(fun arg -> arg = 1)
//    

//initialized (Changed)

//changed.Trigger(1)