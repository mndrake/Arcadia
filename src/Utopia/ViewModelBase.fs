namespace Utopia.ViewModel

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows
open System.Windows.Input

type ObservableObject() as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    
    member this.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])
    
    member this.PropertyChanged = propertyChangedEvent.Publish
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = this.PropertyChanged

type ViewModelBase() as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    abstract ValidatedProperties : string [] with get
    override this.ValidatedProperties = [||]
    abstract GetValidationError : string -> string
    override this.GetValidationError(propertyName) = null
    abstract Error : string with get
    override this.Error with get () = null
    abstract OnDispose : unit with get
    override this.OnDispose with get () = ()
    member this.IsValid with get () = this.ValidatedProperties |> Seq.forall(fun p -> this.GetValidationError(p) = null)
    
    member x.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| x
                                        new PropertyChangedEventArgs(propertyName) |])
    
    member this.Dispose() = this.OnDispose
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
    
    interface IDisposable with
        member this.Dispose() = this.Dispose()
    
    interface IDataErrorInfo with
        member I.Error with get () = this.Error
        member I.Item with get (propertyName) = this.GetValidationError(propertyName)

type RelayCommandCanExecuteDelegate = delegate of obj -> bool

type RelayCommandActionDelegate = delegate of obj -> unit

type RelayCommand(canExecute : RelayCommandCanExecuteDelegate, action : RelayCommandActionDelegate) = 
    let event = new DelegateEvent<EventHandler>()
    interface ICommand with
        
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        
        member x.CanExecute(arg) = canExecute.Invoke(arg)
        member x.Execute(arg) = action.Invoke(arg)

type ActionDelegate = delegate of unit -> unit

type ActionCommand(f : ActionDelegate) = 
    inherit RelayCommand((fun _ -> true), (fun _ -> f.Invoke()))

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
