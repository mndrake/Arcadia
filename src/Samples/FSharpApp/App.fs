namespace FSharpApp
open System
open System.Windows

open FSharpApp.ViewModels

module Main = 
    [<STAThread>]
    (Application.LoadComponent(new Uri("App.xaml", UriKind.Relative)) :?> Application).Run() |> ignore