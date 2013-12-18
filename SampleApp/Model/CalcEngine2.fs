namespace SampleApp.Model

open Utopia

[<CLIMutable>]
type OrderItem = { Name : string; Price : float ; Number : int }

type CalcEngine2() =
    inherit CalculationEngine()

    member this.Items = this.AddInput ([] : OrderItem list)

    member this.Cost = ()
    