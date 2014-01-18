open System
open System.Threading
open System.Diagnostics

type Agent<'T> = MailboxProcessor<'T>

let agent = 
    Agent.Start(fun a ->
        let rec loop() =
            async {
                let! msg = a.Receive()
                do msg |> Seq.iter (printfn "Message: %s")
                return! loop()
            }
        loop()
        )

let post message = agent.Post message

type Value =
    | Valid of int
    | Dirty
    | Pending

let getValue =
    function
    | Valid v -> v
    | _ -> failwith "not a valid number"

type INode =
    abstract Eval : Async<Value>
    abstract SetValue : int -> unit
    abstract Value : Value
    abstract Changed : IEvent<unit>
    abstract Name : string
    abstract AsyncCalc : unit -> unit

[<AbstractClass>]
type NodeBase(name) as this =
    abstract Eval : Async<Value>
    abstract SetValue : int -> unit
    abstract Value : Value
    member this.Name = name
    abstract Changed : IEvent<unit>
    member this.AsyncCalc() =
        this.Eval |> Async.Ignore |> Async.Start

    interface INode with
        member I.Eval = this.Eval
        member I.Name = name
        member I.SetValue(v) = this.SetValue(v)
        member I.Value = this.Value
        member I.Changed = this.Changed
        member I.AsyncCalc() = this.AsyncCalc()

type Input(v : int, name) =
    inherit NodeBase(name)
    let value = ref (Valid v)
    let changed = Event<unit>()
    override I.Eval = async { post ([sprintf "eval Input %s" name]); return !value }
    override I.SetValue(v) = value := Valid(v)
                             changed.Trigger()
    override I.Value with get() = !value
    override I.Changed = changed.Publish

type Output(n1 : INode, n2 : INode, f, name) as this =
    inherit NodeBase(name)
    let value = ref Dirty
    let changed = Event<_>()

    let agent = 
        Agent.Start(fun a ->
            let rec loop() =
                async {
                    let! msg = a.Receive()
                    do msg |> Seq.iter (printfn "Message: %s")
                    return! loop() }
            loop())
            
    let isValid (v:Value) =
        match v with
        |Valid v -> true
        |_ -> false

    let isDirty (v:Value) =
        match v with
        |Dirty -> true
        |_ -> false

    let isPending (v:Value) =
        match v with
        |Pending -> true
        |_ -> false

    do [n1;n2] |> Seq.iter (fun n -> n.Changed.Add(fun _ -> value := Dirty; changed.Trigger()))
       changed.Publish.Add(fun _ -> async { let! v = this.Eval in post ([sprintf "node: %s  ReEval node value %A" this.Name v]) } 
                                    |> Async.Start)

    override I.Changed = changed.Publish
    override I.Value with get() = !value
    override I.SetValue(v) = failwith "cannot set output node"
    override I.Eval = 
        async {
            match !value with
            |Valid(v) -> 
                if (isValid n1.Value) && (isValid n2.Value) then
                    post [(sprintf "node: %s  do nothing -- valid" this.Name)
                          (sprintf "node: %s  n1: %A n2: %A" this.Name n1.Value n2.Value)]
                else
                    value := Dirty
                    changed.Trigger()

            |Pending -> 
                post [(sprintf  "node: %s  pending" this.Name)
                      (sprintf  "node: %s  n1: %A n2: %A" this.Name n1.Value n2.Value)]

            |Dirty ->
                post [(sprintf  "node: %s  output node dirty" this.Name)]
                value := Pending
                changed.Trigger()
                if [n1;n2] |> Seq.forall (fun n -> isValid n.Value) then
                    value := Valid <| f (getValue(n1.Value)) (getValue(n2.Value))
                    changed.Trigger()
                    post [(sprintf  "node: %s  output node dirty done" this.Name)]
                
                else [n1; n2] |> Seq.iter(fun n -> if isDirty(n.Value) then n.AsyncCalc())
                

            return !value }
                                    

let input name v = Input(v,name) :> INode
let func name n1 n2 f = Output(n1, n2, f, name) :> INode
let setValue (n:INode) v = n.SetValue(v)

let i1 = input "i1" 1
let i2 = input "i2" 3
let i3 = input "i3" 5

let n1 = func "n1" i1 i2 (fun x1 x2 -> post [(sprintf "*** eval n1, thread %i" Thread.CurrentThread.ManagedThreadId)] ; x1+x2)
let n2 = func "n2" i2 i3 (fun x1 x2 -> post [(sprintf "*** eval n2, thread %i" Thread.CurrentThread.ManagedThreadId)] ; x1+x2)
let n3 = func "n3" n1 n2 (fun x1 x2 -> post [(sprintf "*** eval n3, thread %i" Thread.CurrentThread.ManagedThreadId)] ; x1+x2)

let evalAsync (node : INode) =
    async { let! v = node.Eval in post [(sprintf "node: %s value %A" node.Name v)] } |> Async.Start

evalAsync n3

//setValue i1 100