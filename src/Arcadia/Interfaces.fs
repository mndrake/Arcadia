namespace Arcadia

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Diagnostics.CodeAnalysis
open System.Threading

type ChangedEventHandler = delegate of sender:obj * e:EventArgs -> unit
type CancelledEventHandler = delegate of sender:obj * e:EventArgs -> unit

type NodeStatus =
    | Dirty = 0
    | Processing = 1
    | Valid = 2

[<ExcludeFromCodeCoverage>]
type Message = 
    | Cancelled
    | Changed
    | Eval of AsyncReplyChannel<NodeStatus * obj>
    | Processed
    | AutoCalculation of bool

type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Cancel : unit -> unit
    abstract Reset : unit -> unit
    
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

type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get