namespace Arcadia

open System
open System.Windows.Threading
open System.Collections.Generic

// The F# extension implementation

[<AutoOpen>]
module ICanThrottleExtensionsFSharp = 

    let internal throttle(milliseconds : int, waitForStable : bool, action : Action) =
        let timer : System.Windows.Threading.DispatcherTimer ref = ref null
        let f() =
            if !timer <> null && not waitForStable then ()
            elif !timer <> null then
                (!timer).Stop()
                timer := null
            timer := new DispatcherTimer(Interval = TimeSpan.FromMilliseconds(float milliseconds))
            (!timer).Tick.AddHandler(fun sender args ->
                (!timer).Stop()
                timer := null
                action.Invoke())
            (!timer).Start()
        f

    type Arcadia.ICanThrottle<'T> with

        member this.Throttle<'T>(milliseconds:int, ?waitForStable) =
            match waitForStable with
            |Some w -> this.SetThrottler(fun a -> Action(throttle(milliseconds, w, a)))
            |None -> this.SetThrottler(fun a -> Action(throttle(milliseconds, false, a)))

// The C# extension implementation

[< System.Runtime.CompilerServices.ExtensionAttribute >]
[< AutoOpen >]
type ICanThrottleExtensionsCSharp = 
    [< System.Runtime.CompilerServices.ExtensionAttribute >]
    static member Throttle(this:ICanThrottle<'T>, milliseconds,waitForStable) = this.Throttle(milliseconds,waitForStable)
    [< System.Runtime.CompilerServices.ExtensionAttribute >]
    static member Throttle(this:ICanThrottle<'T>, milliseconds) = this.Throttle(milliseconds)

[<AutoOpen>]
module SetableListExtensionsFSharp =

    type System.Collections.Generic.IList<'T> with
        member this.RemoveAll<'T>(remove : IEnumerable<'T>) =
            for r in remove do
                this.Remove(r) |> ignore

        member this.AddRange<'T>(add : IEnumerable<'T>) =
            for r in add do
                this.Add(r) |> ignore