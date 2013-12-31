namespace Utopia

open System
open System.Collections.Generic
open System.ComponentModel

type ChangedEventHandler = delegate of obj * EventArgs -> unit

type ICalculationHandler = 
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Cancel : unit -> unit

type INode = 
    abstract Calculation : ICalculationHandler with get    
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract DependentNodes : INode [] with get
    abstract Dirty : bool with get
    abstract Eval : Async<obj> with get
    abstract ID : string with get
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
    abstract Nodes : List<INode> with get
