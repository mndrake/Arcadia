namespace Arcadia

open System
open System.ComponentModel
open Helpers

type InputNode<'U>(calculationHandler, id, ?initialValue) as this = 
    inherit NodeBase<'U>(calculationHandler, id, initialValue)
    let changed = new Event<ChangedEventHandler, EventArgs>()
    let cancelled = new Event<CancelledEventHandler, EventArgs>()

    let mutable propertyChanged = castAs<INotifyPropertyChanged>(!this.value)

    let agent = 
        MailboxProcessor.Start(fun inbox -> 

            let rec valid() = 
                async { 
                    let! msg = inbox.Receive()
                    match msg with
                    | Eval r -> r.Reply(NodeStatus.Valid, box !this.value)
                                return! valid()
                    | _ -> return! valid()
                    }
            valid())
    
    do
        propertyChanged <- castAs<INotifyPropertyChanged>(!this.value)
        if propertyChanged <> null then 
            propertyChanged.PropertyChanged.Add(fun args -> changed.Trigger(this, EventArgs.Empty))
            
    new (calculationHandler, ?initialValue) = 
        match initialValue with
        |Some v -> InputNode(calculationHandler, "", v)
        |None -> InputNode(calculationHandler, "")

    override this.IsProcessing = false
    override this.Status = NodeStatus.Valid
    
    override this.Evaluate() = agent.PostAndAsyncReply(fun r -> Eval r)
    member this.Id = id
    override this.Value with get() = !this.value
                         and set v = this.value := v
                                     changed.Trigger(this, EventArgs.Empty)

    member this.Calculation = calculationHandler
    [<CLIEvent>]
    override this.Cancelled = cancelled.Publish
    [<CLIEvent>]
    override this.Changed = changed.Publish
    override this.IsDirty = false
    override this.IsInput = true
    member this.ToINode() = this :> INode<'U>
    override this.GetDependentNodes() = [||]