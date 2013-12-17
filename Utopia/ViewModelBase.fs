namespace Utopia.ViewModel

open System
open System.Windows
open System.Windows.Input
open System.ComponentModel
open System.Collections.ObjectModel

type ViewModelBase() = 
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
    
    member x.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| x
                                        new PropertyChangedEventArgs(propertyName) |])
    
    abstract OnDispose : unit with get
    override this.OnDispose with get () = ()
    member this.Dispose() = this.OnDispose
    interface IDisposable with
        member this.Dispose() = this.Dispose()

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