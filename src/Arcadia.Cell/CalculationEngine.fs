namespace Arcadia.Cells

open System
open System.Collections.ObjectModel

/// delegate of cell function with inputs of input cell values ('T) to output value ('U)
type CellFunc<'T, 'U> = delegate of unit -> 'U

/// asynchronous calculation engine
type CalculationEngine() = 

    let cells = Collection<ICell>()

    let getCell(cellId) = 
            cells
            |> Seq.tryFind (fun n -> n.Id=cellId)
            |> function
               | Some n -> n
               | None -> failwith "cell not found"

    let mutable inputCount = 0
    let mutable outputCount = 0
    member this.Cells = cells

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    /// adds an InputCell to the CalculationEngine
    member this.AddInput(value : 'U, cellId : string) = 
        let input = InputCell<'U>(cellId, value)
        cells.Add(input)
        input

    /// adds an InputCell to the CalculationEngine
    member this.AddInput(value : 'U) =
        let cellId = "in" + string inputCount
        inputCount <- inputCount + 1
        this.AddInput(value, cellId)

    /// adds an OutputCell to the CalculationEngine    
    member this.AddOutput(dependentCells : 'N, cellFunction : CellFunc<'T,'U>, cellId : string) =
        let f(t) = cellFunction.Invoke()
        let output = OutputCell<'U>(cellId, dependentCells, f)
        cells.Add(output)
        output

    /// adds an OutputCell to the CalculationEngine
    member this.AddOutput(dependentCells : 'N, cellFunction : CellFunc<unit,'U>) =
        let cellId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.AddOutput(dependentCells, cellFunction, cellId)

    /// get cell by id
    member this.Cell<'U>(cellId) = getCell cellId :?> ICell<'U>
    
    /// evaluates a given calculation cell asynchronously
    static member Evaluate(cell : ICell) = async { cell.Evaluate() |> ignore } |> Async.Start
