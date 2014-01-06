namespace Utopia

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Threading

type ChangedEventHandler = delegate of sender:obj * e:EventArgs -> unit

type NodeStatus =
    | Valid
    | Error
    | Processing
    | Dirty
    | Cancelling

type ICalculationHandler = 
    abstract CancellationToken : CancellationToken with get
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Cancel : unit -> unit
    abstract Reset : unit -> unit

type INode = 
    abstract Calculation : ICalculationHandler with get    
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Status : NodeStatus
    abstract GetDependentNodes : unit -> INode []
    abstract Dirty : bool with get
    abstract Computation : Async<obj> with get
    abstract Id : string with get
    abstract IsInput : bool with get
    abstract Processing : bool with get
    abstract RaiseChanged : unit -> unit
    abstract UntypedValue : obj with get, set
    abstract AsyncCalculate : unit -> unit

type INode<'U> =
    inherit INode
    abstract Value : 'U with get,set

type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get
