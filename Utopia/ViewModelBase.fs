namespace Utopia.ViewModel

open System
open System.Windows
open System.Windows.Input
open System.ComponentModel
open System.Collections.ObjectModel

type ViewModelBase() as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()

    abstract ValidatedProperties : string [] with get
    override this.ValidatedProperties = [||]

    abstract GetValidationError : string -> string
    override this.GetValidationError(propertyName) = null
    
    abstract Error : string
    override this.Error with get() = null

    abstract OnDispose : unit with get
    override this.OnDispose with get () = ()

    member this.IsValid with get () = this.ValidatedProperties |> Seq.forall(fun p -> this.GetValidationError(p) = null)

    member x.OnPropertyChanged propertyName = propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])

    member this.Dispose() = this.OnDispose

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
        

    interface IDisposable with
        member this.Dispose() = this.Dispose()

    interface IDataErrorInfo with
        member I.Error with get() = this.Error
        member I.Item with get(propertyName) = this.GetValidationError(propertyName)



type RelayCommand(canExecute : obj -> bool, action : obj -> unit) = 
    let event = new DelegateEvent<EventHandler>()
    interface ICommand with
        
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        
        member x.CanExecute arg = canExecute(arg)
        member x.Execute arg = action(arg)



type CommandViewModel(displayName : string, command : ICommand) = 
    inherit ViewModelBase()
    member this.DisplayName = displayName
    member this.Command = command

[<AutoOpen>]
module Utils = 
    let inline command f = new RelayCommand((fun canExecute -> true), (fun action -> f()))

type PageViewModel() = 
    inherit ViewModelBase()
    let requestClose = new Event<PageViewModel>()
    abstract Name : string with get
    override x.Name = ""
    member x.RequestClose = requestClose.Publish
    member x.OnRequestClose() = requestClose.Trigger(x)
    member x.CloseCommand = new RelayCommand((fun canExecute -> true), (fun action -> x.OnRequestClose()))