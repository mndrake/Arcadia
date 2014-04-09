namespace Arcadia.Eventless

open System
open System.Collections
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Threading.Tasks

type ListEventArgs(value : int) = 
    inherit EventArgs()
    member this.Value = value

type ListEventHandler = delegate of sender:obj * e:ListEventArgs -> unit

type IEquate<'T> = 
    abstract EqualityComparer : Func<'T, 'T, bool> with get, set

type IGetable = 
    [<CLIEvent>] abstract Changed : IEvent<EventHandler, EventArgs> with get

type IGetable<'T> = 
    inherit IGetable
    abstract Value : 'T with get

type IComputed<'T> = 
    abstract Dispose : unit -> unit
    abstract Value : 'T with get

type ISetable<'T> = 
    inherit IGetable<'T>
    abstract Value : 'T with set

type ISetableList<'T> = 
    inherit IGetable<IList<'T>>
    inherit IList<'T>   
    [<CLIEvent>] abstract Added : IEvent<ListEventHandler, ListEventArgs> with get
    [<CLIEvent>] abstract Updated : IEvent<ListEventHandler, ListEventArgs> with get
    [<CLIEvent>] abstract Removed : IEvent<ListEventHandler, ListEventArgs> with get   
    [<CLIEvent>] abstract Cleared : IEvent<EventHandler, EventArgs> with get

type ICanThrottle<'T> =
    abstract SetThrottler : Func<Action, Action> ->  'T

type IBindsTo<'T> =
    abstract Bind : 'T -> unit

module SetableListExtensions = 
    type System.Collections.Generic.IList<'T> with
        member this.RemoveAll<'T>(remove : IEnumerable<'T>) = 
            for r in remove do
                this.Remove(r) |> ignore
        member this.AddRange<'T>(add : IEnumerable<'T>) =
            for r in add do
                this.Add(r)

[<Sealed>]
type RecursiveModificationException() = 
    inherit Exception("Recursive modification of Setable")

/// Intended for use as a singleton, to allow broadcasting events to the
/// listeners at the top of the stack, per-thread
type ListenerStack<'T>() = 
    let Stack = new ThreadLocal<_>(fun () -> new Stack<Action<'T>>())
    member this.Push(listener : Action<'T>) = Stack.Value.Push(listener)
    member this.Pop() = Stack.Value.Pop()
    member this.Notify(obs : 'T) = 
        if (Stack.Value.Count <> 0) then FuncConvert.ToFSharpFunc (Stack.Value.Peek()) (obs)

type Void =
    struct
    end

module ComputedStack = 
    let EmptySubscriptions = new HashSet<IGetable>()
    let Listeners = new ListenerStack<IGetable>()

type Forwarder<'TImpl, 'TValue when 'TImpl :> IEquate<'TValue> and 'TImpl :> IGetable>(impl : 'TImpl) = 
    let _impl = impl
    let changed = new Event<EventHandler, EventArgs>()
    member this.Impl = _impl
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    member this.EqualityComparer 
        with get () = _impl.EqualityComparer
        and set v = _impl.EqualityComparer <- v
    
    interface IGetable with
        [<CLIEvent>]
        member I.Changed = changed.Publish
    
    interface IEquate<'TValue> with
        
        member I.EqualityComparer 
            with get () = _impl.EqualityComparer
            and set v = _impl.EqualityComparer <- v

[<Sealed>]
type Setable<'T>(value : 'T) = 

    let mutable _value = value
    let mutable _changing = false

    let changed = new Event<EventHandler, EventArgs>()

    new() = Setable(Unchecked.defaultof<'T>)

    member val EqualityComparer : Func<'T,'T,bool> = Func<'T,'T,bool>(fun a b -> Setable.DefaultEqualityComparer(a,b)) with get, set
    
    member this.Value 
        with get () = 
            ComputedStack.Listeners.Notify(this)
            _value : 'T
        and set (v : 'T) = 
            if not <| this.EqualityComparer.Invoke(_value, v) then
                if _changing then raise <| new RecursiveModificationException()
                _value <- v
                _changing <- true
                changed.Trigger(this, EventArgs.Empty)
                _changing <- false
    
    [<CLIEvent>]
    member this.Changed = changed.Publish
    
    interface IEquate<'T> with
        
        member this.EqualityComparer 
            with get () = this.EqualityComparer
            and set v = this.EqualityComparer <- v
    
    interface ISetable<'T> with
        [<CLIEvent>]
        member this.Changed = this.Changed

        member this.Value 
            with get() = this.Value
             and set v = this.Value <- v
    
    static member DefaultEqualityComparer(a:'T, b:'T) =
        obj.ReferenceEquals(a, b) || (not <| obj.ReferenceEquals(a, null) && obj.Equals(a, b))

type Setable = 
    static member From<'T>(initVal : 'T) : Setable<'T> = new Setable<'T>(initVal)

[<Sealed>]
type Computed<'T>(compute : Func<'T>) as this = 
    inherit Forwarder<Setable<'T>, 'T>(new Setable<'T>())

    let _compute = compute
    let mutable _subscriptions = ComputedStack.EmptySubscriptions
    let mutable _throttledRecompute = Action(fun() -> this.Recompute())

    do this.Recompute()

    member this.SetThrottler(throttler : Func<Action,Action>) =
        _throttledRecompute <- throttler.Invoke(Action(fun() -> this.Recompute()))
        this

    member private this.RecomputeSoon() = _throttledRecompute.Invoke()

    member this.Recompute() = 
        let newSubscriptions = new HashSet<IGetable>()
        ComputedStack.Listeners.Push(fun (o : IGetable) -> newSubscriptions.Add(o) |> ignore)
        let newVal = _compute.Invoke()
        ComputedStack.Listeners.Pop() |> ignore
        this.Impl.Value <- newVal
        newSubscriptions.Remove(this) |> ignore
        newSubscriptions.Remove(this.Impl) |> ignore
        for sub in _subscriptions |> Seq.filter (fun s -> not <| newSubscriptions.Contains(s)) do
            sub.Changed.RemoveHandler(fun _ _ -> this.RecomputeSoon())
        for sub in newSubscriptions.Where(fun s -> not <| _subscriptions.Contains(s)) do
            sub.Changed.AddHandler(fun _ _ -> this.RecomputeSoon())
        _subscriptions <- newSubscriptions
    
    member this.Value = this.Impl.Value
    
    member this.Dispose() = 
        for sub in _subscriptions do
            sub.Changed.RemoveHandler(fun _ _ -> this.RecomputeSoon())
    
    interface IGetable<'T> with
        member this.Value = this.Value
    
    interface IDisposable with
        member this.Dispose() = this.Dispose()

    interface ICanThrottle<Computed<'T>> with
        member this.SetThrottler(throttler) = this.SetThrottler(throttler)

[<Sealed>]
type SetableComputed<'T>(get : Func<'T>, set : Action<'T>) as this = 
    inherit Forwarder<Computed<'T>, 'T>(new Computed<'T>(get))
    
    member this.Value 
        with get () = this.Impl.Value : 'T
        and set (v : 'T) = set.Invoke(v)
    
    interface ISetable<'T> with
        
        member I.Value 
            with get () = this.Value
            and set v = this.Value <- v

[<Sealed>]
type AsyncComputed<'T>(asyncObs : Func<Async<'T>>) as this =
    inherit Forwarder<Setable<'T>,'T>(new Setable<'T>())

    let _compute() = asyncObs.Invoke()
    let _computed : Computed<Void> = Computed.Do(fun () -> this.Recalculate())
    let _invocationCounter = ref 0

    member private this.Recalculate() : unit =
        incr _invocationCounter
        let invocation = !_invocationCounter       
        let result = async { return! _compute() } |> Async.RunSynchronously        
        if (invocation = !_invocationCounter) then
            this.Impl.Value <- result

    member this.Value = this.Impl.Value

    member this.SetThrottler(throttler : Func<Action,Action>) =
        _computed.SetThrottler(throttler) |> ignore        
        this

    interface IGetable<'T> with
        member this.Value = this.Value
    
    interface ICanThrottle<AsyncComputed<'T>> with
        member this.SetThrottler(throttler) = this.SetThrottler(throttler)


and [<Sealed>] Computed =     
    static member Do(compute : Action) : Computed<Void> = 
        new Computed<Void>(fun () -> 
            compute.Invoke()
            Void())    

    static member From<'T>(compute : Func<'T>) : Computed<'T> = new Computed<'T>(compute)
    static member From<'T>(get : Func<'T>, set : Action<'T>) = new SetableComputed<'T>(get, set)
    static member From<'T>(asyncObs : Func<Async<'T>>) = new AsyncComputed<'T>(asyncObs)

[<Sealed>]
type SetableList<'T>(init : IEnumerable<'T>) = 
    let _list = new List<'T>()
    let _updatingIndexes = new Stack<int>()

    let changed = new Event<EventHandler, EventArgs>()
    let added = new Event<ListEventHandler, ListEventArgs>()
    let updated = new Event<ListEventHandler, ListEventArgs>()
    let removed = new Event<ListEventHandler, ListEventArgs>()
    let cleared = new Event<EventHandler, EventArgs>()
    do _list.AddRange(init)
    new() = SetableList<'T>(new List<'T>())
    member val EqualityComparer = Setable<'T>.DefaultEqualityComparer with get, set
    
    member this.Value =
        ComputedStack.Listeners.Notify(this)
        _list
    
    [<CLIEvent>] member this.Changed = changed.Publish
    [<CLIEvent>] member this.Added = added.Publish
    [<CLIEvent>] member this.Updated = updated.Publish
    [<CLIEvent>] member this.Removed = removed.Publish
    [<CLIEvent>] member this.Cleared = cleared.Publish
    
    member this.Add(item) = 
        _list.Add(item)
        this.Writing((fun i -> added.Trigger(this, ListEventArgs(i))), _list.Count - 1)
    
    member this.IndexOf(item) = 
        ComputedStack.Listeners.Notify(this)
        _list.IndexOf(item)
    
    member this.Clear() = 
        _list.Clear()
        this.Writing((fun _ -> cleared.Trigger(this, EventArgs.Empty)), -1)
        cleared.Trigger(this, EventArgs.Empty)
    
    member private this.Writing(also : int -> unit, index : int) = 
        changed.Trigger(this, EventArgs.Empty)
        also index
    
    member this.Insert(index, item) = 
        _list.Insert(index, item)
        this.Writing((fun i -> added.Trigger(this, ListEventArgs(i))), index)
    
    member this.RemoveAt(index) = 
        _list.RemoveAt(index)
        this.Writing((fun i -> removed.Trigger(this, ListEventArgs(i))), index)
    
    member this.Item 
        with get (index) = 
            ComputedStack.Listeners.Notify(this)
            _list.[index]
        and set index value = 
            if not <| this.EqualityComparer(_list.[index], value) then 
                if _updatingIndexes.Contains(index) then 
                    raise <| new RecursiveModificationException()
                _list.[index] <- value
                _updatingIndexes.Push(index)
                try 
                    this.Writing((fun i -> updated.Trigger(this, ListEventArgs(i))), index)
                finally
                    _updatingIndexes.Pop() |> ignore
    
    member this.Contains(item) = 
        ComputedStack.Listeners.Notify(this)
        _list.Contains(item)
    
    member this.CopyTo(array, arrayIndex) = 
        ComputedStack.Listeners.Notify(this)
        _list.CopyTo(array, arrayIndex)
    
    member this.Count = 
        ComputedStack.Listeners.Notify(this)
        _list.Count
    
    member this.IsReadOnly = false
    
    member this.Remove(item) = 
        ComputedStack.Listeners.Notify(this)
        let index = _list.IndexOf(item)
        if index = -1 then false
        else 
            this.RemoveAt(index)
            true
    
    member this.GetEnumerator() = 
        ComputedStack.Listeners.Notify(this)
        _list.GetEnumerator() :> IEnumerator<'T>
    
    interface IEnumerable with
        member this.GetEnumerator() = this.GetEnumerator() :> IEnumerator
    
    interface ISetableList<'T> with    
        member this.Item 
            with get index = this.[index]
            and set index value = this.[index] <- value        
        member this.IndexOf(item) = this.IndexOf(item)
        member this.Insert(index, item) = this.Insert(index, item)        
        [<CLIEvent>] member this.Changed = this.Changed        
        [<CLIEvent>] member this.Updated = this.Updated        
        [<CLIEvent>] member this.Removed = this.Removed        
        [<CLIEvent>] member this.Added = this.Added        
        [<CLIEvent>] member this.Cleared = this.Cleared        
        member this.Clear() = this.Clear()
        member this.Count = this.Count
        member this.RemoveAt(index) = this.RemoveAt(index)
        member this.IsReadOnly = this.IsReadOnly
        member this.Add(item) = this.Add(item)
        member this.Contains(item) = this.Contains(item)
        member this.CopyTo(array, arrayIndex) = this.CopyTo(array, arrayIndex)
        member this.Remove(item) = this.Remove(item)
        member this.GetEnumerator() = this.GetEnumerator()
        member this.Value with get () = this.Value :> IList<'T>


module Binding =
    let Log = new ListenerStack<Action>()
    let EmptyAction = Action(fun () -> ())
    let CaptureUnbind(bindingActivity) =
        try
            let multicastUnbinders : Action ref = ref null
            Log.Push(fun nestedUnbinder -> multicastUnbinders := nestedUnbinder)
            bindingActivity()
            if !multicastUnbinders = null then
                !multicastUnbinders
            else
                EmptyAction
        finally
            Log.Pop() |> ignore