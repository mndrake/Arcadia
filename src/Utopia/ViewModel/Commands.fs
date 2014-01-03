namespace Utopia.ViewModel

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows
open System.Windows.Input

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

[<AutoOpen>]
module Utils = 
    let inline command f = new RelayCommand((fun canExecute -> true), (fun action -> f()))