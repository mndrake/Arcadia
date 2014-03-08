namespace Arcadia
open System

type CalculationNode<'T>() as this =
    inherit CalculationEngine()
    do ()

    let mutable dependentNodes : INode<'T> option = None
    let mutable outNode : INode<'T> option = None

    interface INode<'T> with

        member I.Value = 


