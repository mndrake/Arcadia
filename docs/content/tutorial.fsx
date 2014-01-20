(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Introduction to Arcadia
=======================

*)
#r "Arcadia.dll"
open Arcadia

let ce = CalculationEngine()

let in1 = ce.AddInput(1)
let in2 = ce.AddInput(1)
let out1 = ce.AddOutput((in1,in2), fun (x,y) -> x+y)
