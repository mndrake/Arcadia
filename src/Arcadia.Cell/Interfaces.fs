namespace Arcadia.Cells

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Threading

/// the status of InputCells and OutputCells
type CellStatus =
    | Dirty = 0
    | Processing = 1
    | Cancelled = 2
    | Error = 3
    | Valid = 4

// event arguments for an objects Changed event
type ChangedEventArgs(status:CellStatus) =
    inherit EventArgs()
    member this.Status = status

/// event handler for an objects Changed event
type ChangedEventHandler = delegate of sender:obj * e:ChangedEventArgs -> unit

/// cell messages used in internal MailboxProcessor
type internal Message = 
    | Cancelled
    | Changed
    | Error
    | Eval of AsyncReplyChannel<CellStatus * obj>
    | Processed
    
/// untyped interface of InputCell and OutputCell
type ICell = 
    abstract Evaluate : unit -> Async<CellStatus * obj>
    abstract UntypedValue : obj with get, set
    abstract Id : string with get
    abstract Status : CellStatus with get
    abstract IsDirty : bool with get
    abstract GetDependentCells : unit -> ICell []
    abstract IsInput : bool with get
    abstract IsProcessing : bool with get
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, ChangedEventArgs> with get

/// typed interface for InputCell and OutputCell
type ICell<'U> =
    inherit ICell
    abstract Value : 'U with get,set

/// interface for CalculationEngine
type ICalculationEngine = 
    abstract Cells : Collection<ICell> with get