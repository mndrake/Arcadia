namespace Arcadia.ViewModel

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows
open System.Windows.Input

// lightweight model class (ObservableObject) and view model base class (ViewModelBase) for MVVM framework
// the Utopia CalculationEngine does implement ObservableObject, which is just a simple implementation of INotifyPropertyChanged
type IObservableObject = 
    inherit INotifyPropertyChanged
    
    /// handler for PropertyChanged event
    abstract OnPropertyChanged : string -> unit
    
    /// triggers PropertyChangedEvent with a given propertyName
    abstract RaisePropertyChanged : string -> unit

type ObservableObject() as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    do (this :> INotifyPropertyChanged).PropertyChanged.Add(fun arg -> this.OnPropertyChanged(arg.PropertyName))
    abstract OnPropertyChanged : string -> unit
    override this.OnPropertyChanged(propertyName) = ()
    
    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this
                                        new PropertyChangedEventArgs(propertyName) |])
    
    interface IObservableObject with
        member I.OnPropertyChanged(propertyName) = this.OnPropertyChanged(propertyName)
        member I.RaisePropertyChanged(propertyName) = this.OnPropertyChanged(propertyName)
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish

type IModelBase = 
    inherit IObservableObject
    inherit IDataErrorInfo
    abstract GetValidationError : string -> string
    abstract ValidatedProperties : string [] with get

type ModelBase() as this = 
    inherit ObservableObject()
    abstract Error : string with get
    override this.Error with get () = null
    abstract GetValidationError : string -> string
    override this.GetValidationError(propertyName) = null
    abstract ValidatedProperties : string [] with get
    override this.ValidatedProperties = [||]
    
    interface IModelBase with
        member I.GetValidationError(propertyName) = this.GetValidationError(propertyName)
        member I.ValidatedProperties = this.ValidatedProperties
    
    interface IDataErrorInfo with
        member I.Error with get () = this.Error
        member I.Item with get (propertyName) = this.GetValidationError(propertyName)

type IViewModelBase = 
    inherit IModelBase
    inherit IDisposable
    abstract OnDispose : unit with get
    abstract IsValid : bool with get

type ViewModelBase() as this = 
    inherit ModelBase()
    abstract OnDispose : unit with get
    override this.OnDispose with get () = ()
    member this.IsValid with get () = this.ValidatedProperties |> Seq.forall(fun p -> this.GetValidationError(p) = null)
    member this.Dispose() = this.OnDispose
    
    interface IViewModelBase with
        member I.IsValid = this.IsValid
        member I.OnDispose = this.OnDispose
    
    interface IDisposable with
        member I.Dispose() = this.Dispose()

type IPageViewModel = 
    inherit IViewModelBase
    abstract Name : string with get
    abstract RequestClose : IEvent<IPageViewModel> with get
    abstract OnRequestClose : unit -> unit
    abstract CloseCommand : RelayCommand with get

type PageViewModel() as this = 
    inherit ViewModelBase()
    let requestClose = new Event<IPageViewModel>()
    abstract Name : string with get
    override x.Name = ""
    member x.RequestClose = requestClose.Publish
    member x.OnRequestClose() = requestClose.Trigger(x)
    member x.CloseCommand = new RelayCommand((fun canExecute -> true), (fun action -> x.OnRequestClose()))
    interface IPageViewModel with
        member I.Name = this.Name
        member I.RequestClose = this.RequestClose
        member I.OnRequestClose() = this.OnRequestClose()
        member I.CloseCommand = this.CloseCommand

type CommandViewModel(displayName : string, command : ICommand) = 
    inherit ViewModelBase()
    member this.DisplayName = displayName
    member this.Command = command
