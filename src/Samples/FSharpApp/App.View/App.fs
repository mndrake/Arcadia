namespace FSharpApp.View
open System
open System.Windows
open System.Windows.Input
open System.Windows.Data
open System.Windows.Controls
open FSharpx
open Helpers
open FSharpApp.ViewModel

type GraphViewType = XAML<"GraphView.xaml">

type VertexDataTemplateSelector() = 
    inherit DataTemplateSelector()
    override this.SelectTemplate(item : obj, container : DependencyObject) : DataTemplate = 
        let element = castAs<FrameworkElement>(container)
        if (element <> null && item <> null && (item |> isType<CustomVertex>)) then 
            let vertexItem = item :?> CustomVertex
            if (vertexItem.IsInput) then element.FindResource("InputVertexTemplate") |> castAs<DataTemplate>
            else element.FindResource("OutputVertexTemplate") |> castAs<DataTemplate>
        else null

type SubmitTextBox() as this =
    inherit TextBox()
    do
        let previewKeyDown (sender : obj) (e : KeyEventArgs) =
                if (e.Key = Key.Enter) then
                    let be = this.GetBindingExpression(TextBox.TextProperty)
                    if be <> null then be.UpdateSource()
        this.PreviewKeyDown.AddHandler(fun sender e -> previewKeyDown sender e)
    
type GraphView() = 
    inherit UserControl()
    let view = new GraphViewType()
    do base.Content <- view.Root

module Main = 
    [<STAThread>]
    (Application.LoadComponent(new Uri("App.xaml", UriKind.Relative)) :?> Application).Run() |> ignore