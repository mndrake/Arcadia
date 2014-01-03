namespace FSharpApp.Views

open System.Windows.Controls
open FSharpx

// 'code-behind' for GraphView.xaml

type OrderGraphViewType = XAML<"Views\OrderGraphView.xaml">
    
type OrderGraphView() = 
    inherit UserControl()
    let view = new OrderGraphViewType()
    do base.Content <- view.Root