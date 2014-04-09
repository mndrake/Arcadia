namespace FSharpApp.Models

open System.Collections.Generic
open System.Threading
open Arcadia
open System

type CollectionCalculationEngine() as this =
    inherit CalculationEngine()

    do        
        // helper functions to add input/output nodes
        let inline input x = this.AddInput x
        let inline output nodes f = this.AddOutput(nodes, f)

        // input nodes
        let in0 = input 1

        ()

