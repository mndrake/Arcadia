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

type SimpleCalculationEngine() as x =
    inherit CalculationEngine()

    do        
        // input nodes
        let in0 = x.Setable 1
        let in1 = x.Setable 1
        let in2 = x.Setable 1
        let in3 = x.Setable 1
        let in4 = x.Setable 1
        let in5 = x.Setable 1
        let in6 = x.Setable 1
        let in7 = x.Setable 1
        let in8 = x.Setable 1
        let in9 = x.Setable 1
        let in10 = x.Setable 1
        let in11 = x.Setable 1
        let in12 = x.Setable 1
        let in13 = x.Setable 1

        // main calculation chain
        let out0 = x.Computed(fun () -> add2 !!in0 !!in1)
        let out1 = x.Computed(fun () -> add2 !!in2 !!in3)
        let out2 = x.Computed(fun () -> add3 !!in4 !!in5 !!in6)
        let out3 = x.Computed(fun () -> add2 !!in7 !!in8)
        let out4 = x.Computed(fun () -> add2 !!out1 !!out2)
        let out5 = x.Computed(fun () -> add2 !!out0 !!out3)
        let out6 = x.Computed(fun () -> add2 !!in9 !!in10)
        let out7 = x.Computed(fun () -> add2 !!in11 !!in12)
        let out8 = x.Computed(fun () -> add2 !!out4 !!out6)
        let out9 = x.Computed(fun () -> add3 !!out5 !!out7 !!out8)

        // secondary calculation chain
        let out10 = x.Computed(fun () -> add2 !!out0 !!out5)
        let out11 = x.Computed(fun () -> !!in13)

        x.Calculation.Automatic <- true