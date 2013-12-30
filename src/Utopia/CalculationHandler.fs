namespace Utopia

open System
open System.Collections.Generic

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
