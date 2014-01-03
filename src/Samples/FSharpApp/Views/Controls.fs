namespace FSharpApp.Views
open System.Windows
open System.Windows.Input
open System.Windows.Controls
open Utopia.Helpers
open FSharpApp.ViewModels

type VertexDataTemplateSelector() = 
    inherit DataTemplateSelector()
    override this.SelectTemplate(item : obj, container : DependencyObject) : DataTemplate = 
        let element = castAs<FrameworkElement>(container)
        if (element <> null && item <> null && (item |> isType<INodeVertex>)) then 
            let vertexItem = item :?> INodeVertex
            if (vertexItem.Node.IsInput) then element.FindResource("InputVertexTemplate") |> castAs<DataTemplate>
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