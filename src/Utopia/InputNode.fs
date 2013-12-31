namespace Utopia

open Helpers
open System
open System.ComponentModel

type InputNode<'U>(calculationHandler, id, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)
       
    let mutable propertyChanged = castAs<INotifyPropertyChanged>(!this.initValue)
    
    do 
        this.dirty := false
        this.Calculation.Changed.Add(fun args -> 
            match this.Calculation.Automatic with
            | true -> this.RaiseChanged()
            | false -> ())

        propertyChanged <- castAs<INotifyPropertyChanged>(!this.initValue)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun args -> this.RaiseChanged())   

    override this.DependentNodes = [||]
    override this.Eval = async { return box this.Value }
    override this.IsInput = true
    
    override this.Value 
        with get () = !this.initValue
        and set v = 
            this.initValue := v
            propertyChanged <- castAs<INotifyPropertyChanged>(!this.initValue)
            if propertyChanged <> null then 
                propertyChanged.PropertyChanged.Add(fun args -> this.RaiseChanged())          
            this.RaiseChanged()