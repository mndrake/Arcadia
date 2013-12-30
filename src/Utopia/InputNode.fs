namespace Utopia

open Helpers
open System
open System.ComponentModel

type InputNode<'U>(calculationHandler, id, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id)
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let dirty = ref false
    
    let initValue = 
        match initialValue with
        | Some v -> ref v
        | None -> ref Unchecked.defaultof<'U>
    
    let processing = ref false
    let mutable propertyChanged = castAs<INotifyPropertyChanged>(!initValue)
    
    do 
        this.Calculation.Changed.Add(fun args -> 
            match this.Calculation.Automatic with
            | true -> changed.Trigger(this, EventArgs.Empty)
            | false -> ())
        propertyChanged <- castAs<INotifyPropertyChanged>(!initValue)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun args -> changed.Trigger(this, EventArgs.Empty))
    
    [<CLIEvent>]
    override this.Changed = changed.Publish
    
    override this.DependentNodes = [||]
    override this.Dirty = !dirty
    override this.Eval = async { return box this.Value }
    override this.IsInput = true
    override this.Processing = !processing
    
    override this.Value 
        with get () = !initValue
        and set v = 
            initValue := v
            propertyChanged <- castAs<INotifyPropertyChanged>(!initValue)
            if propertyChanged <> null then 
                propertyChanged.PropertyChanged.Add(fun args -> changed.Trigger(this, EventArgs.Empty))
            changed.Trigger(this, EventArgs.Empty)
    
    override this.Update() = changed.Trigger(this, EventArgs.Empty)
    member this.PropertyChanged = propertyChanged.PropertyChanged
