namespace Arcadia

open System


type Forwarder<'TImpl, 'TValue when 'TImpl :> IEquate<'TValue> and 'TImpl :> IGetable>(impl : 'TImpl) = 
    member this.Impl = impl

    member this.EqualityComparer
        with get() = impl.EqualityComparer
         and set v = impl.EqualityComparer <- v

    [<CLIEvent>]
    member this.Changed = impl.Changed
    member this.OnChanged(status) = impl.OnChanged(status)

    interface IGetable with
        [<CLIEvent>]
        member this.Changed = this.Changed
        member this.OnChanged(status) = this.OnChanged(status)
        member this.Evaluate() = impl.Evaluate()
        member this.Id = impl.Id

    interface IEquate<'TValue> with
        
        member this.EqualityComparer 
            with get() = this.EqualityComparer
             and set v = this.EqualityComparer <- v