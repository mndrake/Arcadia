namespace FSharpApp.Views

open System.Windows.Controls
open FSharpx

// 'code-behind' for GraphView.xaml

type OrderViewType = XAML<"Views\OrderView.xaml">
    
type OrderView() = 
    inherit UserControl()
    let view = new OrderViewType()
    do base.Content <- view.Root