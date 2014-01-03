namespace FSharpApp.Views

open System.Windows.Controls
open FSharpx

// 'code-behind' for GraphView.xaml

type SimpleGraphViewType = XAML<"Views\SimpleGraphView.xaml">
    
type SimpleGraphView() = 
    inherit UserControl()
    let view = new SimpleGraphViewType()
    do base.Content <- view.Root