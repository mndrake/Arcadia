namespace Utopia

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open Microsoft.FSharp.Reflection

type CalculationMode = 
    | Manual
    | Automatic

/// base class for Input/Output nodes and also Graph nodes
[<AbstractClass>]
type NodeBase(id) as this = 
    abstract Parent : CalculationEngine with get
    abstract Changed : IEvent<unit> with get
    abstract DependentNodes : INode [] with get
    abstract Dirty : bool with get
    abstract Processing : bool with get
    abstract Eval : Async<obj> with get
    abstract CurrentValue : obj with get, set
    abstract IsInput : bool with get
    member this.ID = id
    abstract Initialize : unit -> unit
    interface INode with
        member I.Initialize() = this.Initialize()
        member I.IsInput = this.IsInput
        member I.Dirty = this.Dirty
        member I.Parent = this.Parent
        member I.Processing = this.Processing
        member I.Eval = this.Eval
        member I.Changed = this.Changed
        member I.DependentNodes = this.DependentNodes
        member I.ID = this.ID
        
        member I.CurrentValue 
            with get () = this.CurrentValue
            and set v = this.CurrentValue <- v

and INode = 
    abstract Parent : CalculationEngine with get
    abstract Dirty : bool with get
    abstract Eval : Async<obj> with get
    abstract Changed : IEvent<unit> with get
    abstract Processing : bool with get
    abstract DependentNodes : INode [] with get
    abstract IsInput : bool with get
    abstract CurrentValue : obj with get, set
    abstract ID : string with get
    abstract Initialize : unit -> unit

and CalculationEngine() = 
    let nodes = List<INode>()
    let calculationChanged = Event<unit>()
    let calculation = ref CalculationMode.Manual
    let mutable inputCount = 0
    let mutable outputCount = 0

    member this.Nodes = nodes
    
    member this.Calculation 
        with get () = !calculation
        and set v = 
            calculation := v
            calculationChanged.Trigger()
    
    member this.CalculationChanged = calculationChanged.Publish
    
    member this.AddInput(value : 'U, ?nodeID : string) = 
        let id = 
            match nodeID with
            | Some i -> i
            | None -> "in" + string inputCount
        inputCount <- inputCount + 1
        let input = Input<'U>(this, value, id)
        nodes.Add(input)
        input
    
    member this.AddOutput(dependentNodes : 'N, asyncFun : 'T -> Async<'U>, ?nodeID : string) = 
        let id = 
            match nodeID with
            | Some i -> i
            | None -> "out" + string outputCount
        outputCount <- outputCount + 1
        let output = Output<'N, 'T, 'U>(this, dependentNodes, asyncFun, id)
        nodes.Add(output)
        output

and Input<'U>(parent : CalculationEngine, v : 'U, id) = 
    inherit NodeBase(id)
    let changed = new Event<unit>()
    let initValue = ref v
    let dirty = ref false
    let processing = ref false
    
    do 
        parent.CalculationChanged.Add(fun () -> 
            match parent.Calculation with
            | CalculationMode.Automatic -> changed.Trigger()
            | CalculationMode.Manual -> ())
    
    override this.Parent = parent
    override this.Processing = !processing
    
    override this.CurrentValue 
        with get () = box <| !initValue
        and set v = 
            this.Value <- match this.Converter with
                          | Some f -> f(string v)
                          | None -> (unbox <| v)
    
    override this.Initialize() = changed.Trigger()
    override this.Changed = changed.Publish
    override this.DependentNodes = [||]
    override this.Dirty = !dirty
    override this.Eval = async { return box this.Value }
    override this.IsInput = true
    member val Converter = None with get, set
    
    member this.Value 
        with get () = !initValue
        and set v = 
            initValue := v
            changed.Trigger()

and Output<'N, 'T, 'U>(parent : CalculationEngine, nodes : 'N, func : 'T -> Async<'U>, id) as this = 
    inherit NodeBase(id)
    let changed = Event<unit>()
    let nodesArray = FSharpValue.GetTupleFields(nodes) |> Array.map(fun x -> x :?> INode)
    let dirty = ref true
    let processing = ref false
    let initValue = ref(Unchecked.defaultof<'U>)
    
    do 
        nodesArray |> Seq.iter(fun n -> 
                          n.Changed.Add(fun () -> 
                              dirty := true
                              changed.Trigger()))
        this.Changed.Add
            (fun () -> 
            if (not !processing) && (this.Parent.Calculation = CalculationMode.Automatic) 
               && (nodesArray |> Seq.forall(fun n -> not n.Dirty)) then 
                async { do! this.Eval |> Async.Ignore } |> Async.Start)
    
    override this.Parent = parent
    override this.Processing = !processing
    override this.Initialize() = changed.Trigger()
    member this.Update() = async { this.Eval |> ignore } |> Async.Start
    override this.DependentNodes = nodesArray
    member this.Value with get () = (this :> INode).Eval |> Async.RunSynchronously
    
    override this.CurrentValue 
        with get () = box <| !initValue
        and set v = failwith "cannot set value of an output node"
    
    override this.Dirty = !dirty
    override this.Changed = changed.Publish
    override this.IsInput = false
    override this.Eval = 
        async { 
            use! cancelHandler = Async.OnCancel
                                     (fun () -> 
                                     processing := false
                                     Debug.Print
                                         (String.Format("-- canceling eval -- ID {0}, Dirty {1}", this.ID, this.Dirty)))
            if !dirty && not !processing then 
                processing := true
                let! arrayValues = nodesArray
                                   |> Seq.map(fun n -> n.Eval)
                                   |> Async.Parallel
                let arrayFunc = fun p -> (FSharpValue.MakeTuple(p, typeof<'T>) :?> 'T) |> func
                let! result = arrayFunc arrayValues
                initValue := result
                Debug.Print
                    (String.Format
                         ("ID {0} VALUE {1} -- THREAD {2}", this.ID, !initValue, Thread.CurrentThread.ManagedThreadId))
                dirty := false
                processing := false
                changed.Trigger()
            while (!processing) do
                do! Async.Sleep(100)
            return box <| !initValue
        }

module Node = 
    let eval(node : INode) = async { node.Eval |> ignore } |> Async.Start
