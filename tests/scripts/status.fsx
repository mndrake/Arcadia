// attempt to implement multiple node state event instead of a simple event trigger

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.ComponentModel
open System.Linq
open System.Threading
open Microsoft.FSharp.Reflection

//#region Helper Methods

module Helpers = 
    /// checks if value is a F# Tuple type
    let isTuple value = 
        match box value with
        | null -> false
        | _ -> Microsoft.FSharp.Reflection.FSharpType.IsTuple(value.GetType())

    /// checks to see if object is of type 'T
    let isType<'T> o =
        match box o with 
        | :? 'T -> true
        | _ -> false
    
    // http://stackoverflow.com/questions/2361851/c-sharp-and-f-casting-specifically-the-as-keyword
    /// F# implementation of the C# 'as' keyword
    let castAs<'T when 'T : null>(o : obj) = 
        match o with
        | :? 'T as res -> res
        | _ -> null

    let OnAnyPropertyChanged (action:unit -> unit) (o:obj) =
        let propertyChanged = castAs<INotifyPropertyChanged>(o)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun _ -> action()) 

//#endregion

//#region Interfaces and Events

// using enum instead of discriminated union to ease consumption by C#

type NodeState = 
    | Dirty
    | Valid
    | Error
    | Processing
    | Cancelling

type NodeStateChangedEventArgs(state : NodeState) =
    inherit EventArgs()
    member this.State = state

type NodeStateChangedEventHandler = delegate of obj * NodeStateChangedEventArgs -> unit

type ChangedEventHandler = delegate of sender:obj * e:EventArgs -> unit

type ICalculationHandler = 
    abstract Automatic : bool with get, set
    [<CLIEvent>] 
    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Cancel : unit -> unit

type INode = 
    abstract Calculation : ICalculationHandler with get    
    [<CLIEvent>] 
//    abstract Changed : IEvent<ChangedEventHandler, EventArgs> with get
    abstract Changed : IEvent<NodeStateChangedEventHandler, NodeStateChangedEventArgs> with get
    abstract State : NodeState
    abstract GetDependentNodes : unit -> INode []
    abstract IsDirty : bool with get
    abstract Computation : Async<obj> with get
    abstract Id : string with get
    abstract IsInput : bool with get
    abstract IsProcessing : bool with get
    abstract RaiseChanged : unit -> unit
    abstract UntypedValue : obj with get, set
    abstract AsyncCalculate : unit -> unit

type INode<'U> = 
    inherit INode
    abstract Value : 'U with get,set

type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : Collection<INode> with get

//#endregion

//#region NodeBase

/// base class for Calculation Nodes
[<AbstractClass>]
type NodeBase<'U>(calculationHandler : ICalculationHandler, id, initialValue) as this = 
    let changed = new Event<NodeStateChangedEventHandler, NodeStateChangedEventArgs>()
    //let changed = new Event<ChangedEventHandler, EventArgs>()

    member val internal state = ref NodeState.Dirty
    //member val internal dirty = ref true
    //member val internal processing = ref false
    
    member val internal initValue = match initialValue with
                                    | Some v -> ref v
                                    | None -> ref Unchecked.defaultof<'U>
    
    abstract GetDependentNodes : unit -> INode []
    abstract Computation : Async<obj> with get
    abstract IsInput : bool with get
    abstract Value : 'U with get, set
    member this.State = !this.state
    member this.ToINode() = this :> INode<'U>
    member this.AsyncCalculate() = async { do! this.Computation |> Async.Ignore } |> Async.Start
    member this.Calculation = calculationHandler
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    member this.IsDirty = !this.state <> NodeState.Valid //!this.dirty
    member this.Id = id
    member this.IsProcessing = !this.state = NodeState.Processing
    member this.RaiseChanged() = changed.Trigger(this, NodeStateChangedEventArgs(!this.state))
    interface INode<'U> with
        member I.AsyncCalculate() = this.AsyncCalculate()
        member I.Calculation = this.Calculation
        
        [<CLIEvent>]
        member I.Changed = this.Changed
        
        member I.GetDependentNodes() = this.GetDependentNodes()
        member I.IsDirty = this.IsDirty
        member I.Computation = this.Computation
        member I.Id = this.Id
        member I.IsInput = this.IsInput
        member I.IsProcessing = this.IsProcessing
        member I.RaiseChanged() = this.RaiseChanged()
        member I.State = this.State
        member I.UntypedValue 
            with get () = box this.Value
            and set v = this.Value <- unbox v
        
        member I.Value 
            with get () = this.Value
            and set v = this.Value <- v

//#endregion

//#region InputNode

open Helpers

type InputNode<'U>(calculationHandler, id, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)
       
    let mutable propertyChanged = castAs<INotifyPropertyChanged>(!this.initValue)
    
    do 
        this.state := NodeState.Valid
        this.Calculation.Changed.Add(fun args -> 
            match this.Calculation.Automatic with
            | true -> this.RaiseChanged()
            | false -> ())

        propertyChanged <- castAs<INotifyPropertyChanged>(!this.initValue)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun args -> this.RaiseChanged())   

    override this.GetDependentNodes() = [||]
    override this.Computation = async { return box this.Value }
    override this.IsInput = true
    override this.Value 
        with get () = !this.initValue
        and set v = 
            this.initValue := v
            propertyChanged <- castAs<INotifyPropertyChanged>(!this.initValue)
            if propertyChanged <> null then 
                propertyChanged.PropertyChanged.Add(fun args -> this.RaiseChanged())          
            this.RaiseChanged()

//#endregion

//#region OutputNode

open Helpers

type OutputNode<'N, 'T, 'U>(calculationHandler, id, nodeInputs : 'N, nodeFunction : 'T -> 'U, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)
    
    // convert input nodes and function into array versions to ease parallel async support
    let nodes = 
        if isTuple nodeInputs then FSharpValue.GetTupleFields(nodeInputs) |> Array.map(fun x -> x :?> INode)
        else [| (box nodeInputs) :?> INode |]
    
    let func = 
        if isTuple nodeInputs then fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> nodeFunction
        else fun p -> (p.[0] :?> 'T) |> nodeFunction
    
    // debug messages
    let msgCancel() = Console.WriteLine("EVAL CANCELLED : ID {0}, Dirty {1}", this.Id, this.IsDirty)
    let msgUpdate() = Console.WriteLine("ID {0} VALUE UPDATED : THREAD {1}", this.Id, Thread.CurrentThread.ManagedThreadId)
    
    // TODO: replace with proper cancellation
    let reprocess = ref false

    /// Evaluates node state of dependent nodes to determine this node state on
    /// dependent node state change
    let getNodesState() =
        if nodes.Any(fun n -> n.State = NodeState.Error) then
            NodeState.Error
        elif nodes.Any(fun n -> n.State = NodeState.Processing) then
            NodeState.Processing
        else
            NodeState.Dirty

    do 
        this.state := NodeState.Dirty
        // hook into dependent(parent) node StateChanged events
        nodes |> Seq.iter(fun n -> 
                     n.Changed.Add(fun args -> 
                        if nodes.Any(fun n -> n.State = NodeState.Error) then
                            this.state := NodeState.Error
                        else
                            this.state := NodeState.Dirty
                        this.RaiseChanged()))

        this.Changed.Add
            (fun args -> 
                // only update value if automatic calculation enabled
                if this.Calculation.Automatic then
                    // if already processing don't reevaluate 
                    // TODO : need to cancel and restart
                    if !this.state <> NodeState.Processing && nodes.All(fun n -> n.State = NodeState.Valid) then
                        async { do! this.Computation |> Async.Ignore } |> Async.Start)
                        
    override this.GetDependentNodes() = nodes.Clone() |> unbox
    
    override this.Computation = 
        async { 
            use! cancelHandler = Async.OnCancel(fun () -> 
                                    if nodes.Any(fun n -> n.State = NodeState.Error) then
                                        this.state := NodeState.Error
                                    else
                                        this.state := NodeState.Dirty
                                    this.RaiseChanged()                                  
                                    msgCancel())
            if !this.state = NodeState.Dirty && nodes.All(fun n -> n.State = NodeState.Valid) then 
                this.state := NodeState.Processing
                let! values = nodes
                              |> Seq.map(fun n -> n.Computation)
                              |> Async.Parallel
                let! result = async { return func values }
                this.initValue := result
                msgUpdate()
                this.state := NodeState.Valid
                this.RaiseChanged()
            while (!this.state = NodeState.Processing) do
                do! Async.Sleep(100)
            return box <| !this.initValue
        }
    
    override this.IsInput = false
    
    override this.Value 
        with get () = !this.initValue
        and set v = invalidArg "Value" "cannot set value of an output node"

//#endregion

//#region CalculationHandler

type CalculationHandler() as this = 
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let automatic = ref false
    
    member this.Automatic 
        with get () = !automatic
        and set v = 
            automatic := v
            changed.Trigger(this, EventArgs.Empty)
    
    member this.Cancel() = Async.CancelDefaultToken()
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    interface ICalculationHandler with
        
        member I.Automatic 
            with get () = this.Automatic
            and set v = this.Automatic <- v
        
        [<CLIEvent>]
        member I.Changed = this.Changed
        
        member I.Cancel() = this.Cancel()

//#endregion

//#region CalculationEngine

type NodeFunc<'T, 'U> = delegate of 'T -> 'U

type CalculationEngine(calculationHandler : ICalculationHandler) as this = 

    let nodes = Collection<INode>()
    let mutable inputCount = 0
    let mutable outputCount = 0
    new() = new CalculationEngine(new CalculationHandler())
    member this.Nodes = nodes
    member this.Calculation = calculationHandler

    // overloaded methods instead of using an F# optional parameter
    // otherwise an F# option would be exposed to CLI
    
    member this.AddInput(value : 'U, nodeId : string) = 
        let input = InputNode<'U>(this.Calculation, nodeId, value)
        nodes.Add(input)
        input

    member this.AddInput(value : 'U) =
        let nodeId = "in" + string inputCount
        inputCount <- inputCount + 1
        this.AddInput(value, nodeId)
    
    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>, nodeId : string) =
        let f(t) = nodeFunction.Invoke(t)
        let output = OutputNode<'N, 'T, 'U>(this.Calculation, nodeId, dependentNodes, f)
        nodes.Add(output)
        output

    member this.AddOutput(dependentNodes : 'N, nodeFunction : NodeFunc<'T,'U>) =
        let nodeId = "out" + string outputCount
        outputCount <- outputCount + 1
        this.AddOutput(dependentNodes, nodeFunction, nodeId)
 
    interface ICalculationEngine with
        member I.Nodes = this.Nodes
        member I.Calculation = this.Calculation
    
    static member Eval(node : INode) = async { node.Computation |> ignore } |> Async.Start
//#endregion

//#region CalculationEngine Example 

//let ce = new CalculationEngine()
//
//let input v = ce.AddInput v
//let output n f = ce.AddOutput(n, NodeFunc(f))
//
//let add2 (x0,x1) =
//    Thread.Sleep 1000
//    let y = x0 + x1
//    printfn "value : %i" y
//    y
//
//let in0 = ce.AddInput 1
//let in1 = ce.AddInput 1
//
//let out0 = output (in0, in1) add2
//
//// run the following a line at a time
//
//out0.AsyncCalculate()
//
//ce.Calculation.Automatic <- true
//
//in0.Value <- 4

//#endregion

//#region Agent Example

let agent1 = MailboxProcessor.Start(fun agent -> 
    let rec loop() = async {
            // Asynchronously wait for the next message
            let! (msg : NodeState * string) = agent.Receive()
            match msg with
                | s, n -> printfn "node: %s status: %A" n s
            return! loop() }
    loop())
    
let agent2 = MailboxProcessor.Start(fun agent ->
    let rec loop() = async {
            //Asynchronously wait for the next message
            let! (msg : NodeState * string) = agent.Receive()
            match msg with
                | s, n -> printfn "node: %s status %A" n s
            return! loop() }
    loop())

statusAgent.Post(Dirty, "out1")

//#endregion