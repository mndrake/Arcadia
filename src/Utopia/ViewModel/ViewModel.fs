namespace Utopia.ViewModel

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows
open System.Windows.Input

// lightweight model class (ObservableObject) and view model base class (ViewModelBase) for MVVM framework
// the Utopia CalculationEngine does implement ObservableObject, which is just a simple implementation of INotifyPropertyChanged


type ObservableObject() as this = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()

    do
        let t = (this :> INotifyPropertyChanged).PropertyChanged.Add(this.OnPropertyChanged)
        ()

    abstract OnPropertyChanged : PropertyChangedEventArgs -> unit

    default this.OnPropertyChanged(args) = ()

    member this.RaisePropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; new PropertyChangedEventArgs(propertyName) |])

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member I.PropertyChanged = propertyChangedEvent.Publish
    


type ViewModelBase() as this = 
    inherit ObservableObject()

    abstract ValidatedProperties : string [] with get
    override this.ValidatedProperties = [||]
    abstract GetValidationError : string -> string
    override this.GetValidationError(propertyName) = null
    abstract Error : string with get
    override this.Error with get () = null
    abstract OnDispose : unit with get
    override this.OnDispose with get () = ()
    member this.IsValid with get () = this.ValidatedProperties |> Seq.forall(fun p -> this.GetValidationError(p) = null)
        
    member this.Dispose() = this.OnDispose
        
    interface IDisposable with
        member this.Dispose() = this.Dispose()
    
    interface IDataErrorInfo with
        member I.Error with get () = this.Error
        member I.Item with get (propertyName) = this.GetValidationError(propertyName)

type PageViewModel() = 
    inherit ViewModelBase()
    let requestClose = new Event<PageViewModel>()
    abstract Name : string with get
    override x.Name = ""
    member x.RequestClose = requestClose.Publish
    member x.OnRequestClose() = requestClose.Trigger(x)
    member x.CloseCommand = new RelayCommand((fun canExecute -> true), (fun action -> x.OnRequestClose()))

type CommandViewModel(displayName : string, command : ICommand) = 
    inherit ViewModelBase()
    member this.DisplayName = displayName
    member this.Command = command