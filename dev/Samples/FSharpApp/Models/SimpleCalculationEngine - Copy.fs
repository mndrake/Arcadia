namespace FSharpApp.Models

open System.Collections.Generic
open System.Threading
open Arcadia
open Arcadia.FSharp
open System

[<AutoOpen>]
module SimpleMethods =

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

type SimpleCalculationEngine() as this =
    inherit CalculationEngine()

    do        
        // helper functions to add input/output nodes (optional)
        let inline input x = this.Setable x
        let inline output f = this.Computed (fun() -> f())

        // input nodes
        let in0 = input 1
        let in1 = input 1
        let in2 = input 1
        let in3 = input 1
        let in4 = input 1
        let in5 = input 1
        let in6 = input 1
        let in7 = input 1
        let in8 = input 1
        let in9 = input 1
        let in10 = input 1
        let in11 = input 1
        let in12 = input 1
        let in13 = input 1

        // main calculation chain
        let out0 = output <| fun () -> add2 !!in0 !!in1
        let out1 = output <| fun () -> add2 !!in2 !!in3
        let out2 = output <| fun () -> add3 !!in4 !!in5 !!in6
        let out3 = output <| fun () -> add2 !!in7 !!in8
        let out4 = output <| fun () -> add2 !!out1 !!out2
        let out5 = output <| fun () -> add2 !!out0 !!out3
        let out6 = output <| fun () -> add2 !!in9 !!in10
        let out7 = output <| fun () -> add2 !!in11 !!in12
        let out8 = output <| fun () -> add2 !!out4 !!out6
        let out9 = output <| fun () -> add3 !!out5 !!out7 !!out8

        // secondary calculation chain
        let out10 = output <| fun () -> add2 !!out0 !!out5

        let out11 = output <| fun () -> !!in13

        this.Initialize()