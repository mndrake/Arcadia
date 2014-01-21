namespace Arcadia

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Threading

/// event handler for an objects Changed event
type ChangedEventHandler = delegate of sender:obj * e:EventArgs -> unit
/// event handler for calculation Cancelled event
type CancelledEventHandler = delegate of sender:obj * e:EventArgs -> unit

/// the status of InputNodes and OutputNodes
type NodeStatus =
    | Dirty = 0
    | Processing = 1
    | Valid = 2

/// node messages used in internal MailboxProcessor
type internal Message = 
    | Cancelled
    | Changed
    | Eval of AsyncReplyChannel<NodeStatus * obj>
    | Processed
    | AutoCalculation of bool

/// interface of CalculationHandler
type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
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
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    [<CLIEvent>]
    abstract Cancelled : IEvent<CancelledEventHandler, EventArgs> with get

/// typed interface for InputNode and OutputNode
type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

/// interface for CalculationEngine
type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get