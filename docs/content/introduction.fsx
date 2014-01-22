(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#r "../../bin/Arcadia.dll"

(**
Introduction to Arcadia
=======================

Arcadia is an asynchronous calculation framework inspired by a discussion by Tobias Gedell on Eden [YouTube](http://www.youtube.com/watch?v=BsOtAXV_URI).  

The main points of the discussion on Eden that stuck with me were :  

1. Laziness and partial recalc  
2. Caching  
3. Asynchronous result production  
4. Automatic parallelization  
5. Optional manual calculation  
6. Cancellation  
(also the NEED ... TO ... REPLACE ... EXCEL)  

Currently I have implemented the above 6 points, although larger scale tests are still needed to test how it scales.  

**TO DO LIST**  
redo  
serialization of ``CalculationEngine``  

Arcadia is implemented using .Net generics so calculation "nodes" do not need to implement just a single numberic value.  Inputs/Outputs can be any POCO/recordset/struct that you want.  

Node Dependency Graph
=====================

Here is a dependency graph with input nodes (green) and output nodes (blue).  We will use this as an illustration of the dependency tree that we will now try to replicate using simple integer based nodes.  

<img src="img/NodeGraph.png" height="300" width="600" />


F# Example - simple integers
============================

First lets define some simple functions to represent some slow running functions.  

*)

open System.Threading

let add2 (x1,x2) = 
    Thread.Sleep 500
    x1 + x2

let add3 (x1,x2,x3) =
    Thread.Sleep 1000
    x1 + x2 + x3

(**
Now lets create a calculation engine that does simple addition at nodes based on the dependency graph we saw earlier. An optional custom ID can be assigned to a node.
If no node ID is given then ``InputNodes`` will be named in0, in1, in2, ... and ``OutputNodes`` will be named out0, out1, out2, ...
*)

open Arcadia

type SimpleCalcEngine() as this =
    inherit CalculationEngine()

    let input v = this.AddInput v
    let output nodes func = this.AddOutput(nodes, NodeFunc(func))

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
    
    // output nodes
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
    let out10 = output (out0,out5) add2

(**
Test out our Calculation Engine
-------------------------------

Create an instance of the calculation engine and turn on automatic calculations.  
*)

let ce = SimpleCalcEngine()

/// print out the status and value of a given node.
let nodeValue(nodeId) = 
    let n = ce.Node(nodeId)
    printfn "%s status:%A value:%i" (n.Id) (n.Status) (n.Value)

nodeValue "out9" // returns "out9 status:Dirty value:0"

ce.Calculation.Automatic <- true

// check again (will need to wait a few seconds while calculations complete)
nodeValue "out9" // returns "out9 status:Valid value:13"

(**
An example of how this can be implemented in an MVVM application can be found on the GitHub site.
*)