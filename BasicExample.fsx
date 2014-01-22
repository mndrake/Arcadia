#r "bin/Arcadia.dll"

open System.Threading

let add2 (x1,x2) = 
    Thread.Sleep 500
    x1 + x2

let add3 (x1,x2,x3) =
    Thread.Sleep 1000
    x1 + x2 + x3

(**
Now lets create a calculation engine that does simple addition at nodes based on the dependency graph we saw earlier.  
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

// lets test it out

let ce = SimpleCalcEngine()

let nodeValue(nodeId) = 
    let n = ce.Node(nodeId)
    printfn "%s status:%A value:%i" (n.Id) (n.Status) (n.Value)

// lets check the value of out9
nodeValue "out9" // returns "out9 status:Dirty value:0"

// enable calculations

ce.Calculation.Automatic <- true

// check again

nodeValue "out9" // returns "out9 status:Valid value:13"