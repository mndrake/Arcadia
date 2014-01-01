namespace FSharpApp.Views

open System.Windows.Controls
open FSharpx

// 'code-behind' for GraphView.xaml

type GraphViewType = XAML<"Views\GraphView.xaml">
    
type GraphView() = 
    inherit UserControl()
    let view = new GraphViewType()
    do base.Content <- view.Root