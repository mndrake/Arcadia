namespace Arcadia

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Threading

/// the status of InputNodes and OutputNodes
type NodeStatus =
    | Dirty = 0
    | Processing = 1
    | Cancelled = 2
    | Error = 3
    | Valid = 4

// event arguments for an objects Changed event
type ChangedEventArgs(status:NodeStatus) =
    inherit EventArgs()
    member this.Status = status

/// event handler for an objects Changed event
type ChangedEventHandler = delegate of sender:obj * e:ChangedEventArgs -> unit

/// node messages used in internal MailboxProcessor
type internal Message = 
    | Cancelled
    | Changed
    | Error
    | Eval of AsyncReplyChannel<NodeStatus * obj>
    | Processed
    | AutoCalculation of bool

/// interface of CalculationHandler
type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<EventHandler, EventArgs> with get
    abstract Cancel : unit -> unit
    abstract Reset : unit -> unit
    
/// untyped interface of InputNode and OutputNode
type INode = 
    abstract Calculation : ICalculationHandler with get
    abstract Evaluate : unit -> Async<NodeStatus * obj>
    abstract UntypedValue : obj with get, set
    abstract Id : string with get
    abstract Status : NodeStatus with get
    abstract AsyncCalculate : unit -> unit
    abstract IsDirty : bool with get
    abstract GetDependentNodes : unit -> INode []
    abstract IsInput : bool with get
    abstract IsProcessing : bool with get
    [<CLIEvent>]
    abstract Changed : IEvent<ChangedEventHandler, ChangedEventArgs> with get

/// typed interface for InputNode and OutputNode
type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

/// interface for CalculationEngine
type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get