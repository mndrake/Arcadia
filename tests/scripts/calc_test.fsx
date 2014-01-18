#r "../../src/Samples/FSharpApp/bin/Debug/Utopia.dll"
#r "../../src/Samples/FSharpApp/bin/Debug/Utopia.MVVM.dll"
#r "../../src/Samples/FSharpApp/bin/Debug/Utopia.Graph.dll"
#r "../../src/Samples/FSharpApp/bin/Debug/FSharpApp.exe"

open FSharpApp.ViewModels

let main = new MainWindowViewModel()

let order = main.PageViewModels.[1] :?> OrderViewModel

order.Order.Tax <- 0.02


//     OrderResult = {TotalUnits = 15;
//                    PreTaxAmount = 230.0;
//                    TaxAmount = 16.1;
//                    TotalAmount = 246.1;};