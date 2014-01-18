#I "../../src/Utopia"
#load "Helpers.fs"
      "Interfaces.fs"
      "NodeBase.fs"
      "InputNode.fs"
      "OutputNode.fs"
      "CalculationHandler.fs"
      "CalculationEngine.fs"

open System.Threading
open Utopia

let ce = new CalculationEngine()

let input v = ce.AddInput(v)

let output n f = ce.AddOutput(n,NodeFunc(f))

let in0 = input 1
let in1 = input 1

let out0 = output (in0, in1) (fun (x,y) -> Thread.Sleep(1000); x+y)

ce.Calculation.Automatic <- true

out0.AsyncCalculate()

out0