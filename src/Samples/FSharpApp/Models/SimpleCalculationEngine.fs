namespace FSharpApp.Models

open System.Collections.Generic
open System.Threading
open Utopia
open System

[<AutoOpen>]
module SimpleMethods =

    // async functions that are used to calculate output nodes
    let add2(x1, x2) = 
            Thread.Sleep 500
            x1 + x2

    let add3(x1, x2, x3) = 
            Thread.Sleep 1000
            x1 + x2 + x3

    let add4(x1, x2, x3, x4) = 
            Thread.Sleep 1500
            x1 + x2 + x3 + x4

type SimpleCalculationEngine() as this =
    inherit CalculationEngine()

    do        
        // helper functions to add input/output nodes
        let inline input x = this.AddInput x
        let inline output nodes f = this.AddOutput(nodes, NodeFunc(f))

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
        let out0 = output (in0,in1) add2
        let out1 = output (in2,in3) add2
        let out2 = output (in4,in5,in6) add3
        let out3 = output (in7,in8) add2
        let out4 = output (out1,out2) add2
        let out5 = output (out0,out3) add2
        let out6 = output (in9,in10) add2
        let out7 = output (in11,in12) add2
        let out8 = output (out4,out6) add2
        let out9 = output (out5,out7,out8) add3

        // secondary calculation chain
        let out10 = output (out0, out5) add2

        let out11 = output in13 (fun (x:int) -> x)

        ()