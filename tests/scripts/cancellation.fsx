open System.Threading

type T() =

    let value = ref 0

    let f (x,y) =
        async {
        Thread.Sleep 2000
        value := x+y }

    let mutable mainToken : CancellationToken option = None
    let mutable combinedCts : CancellationTokenSource option = None

    let computation =
        async {
            printfn "starting"
            use! cancelHandler = Async.OnCancel( fun () -> printfn "cancelled" )
            let! result = f(1,1)
            do printfn "result %i" !value
            }

    member this.CancelRequested =
        match combinedCts with
        | Some t -> Some t.IsCancellationRequested
        | None -> None

    member this.Eval(cancellationToken) =
        let cts = new CancellationTokenSource()
        combinedCts <-
            Some <| CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token)
        Async.Start(computation, combinedCts.Value.Token)

    member this.Computation = computation

    member this.Cancel() = 
        match combinedCts with
        |Some t -> t.Cancel()
        |None -> ()

    member this.Restart() = ()

// test

let mainCts = new CancellationTokenSource()

let t = T()

t.Eval(mainCts.Token)

Thread.Sleep 1000

t.Cancel()




//    type Microsoft.FSharp.Control.Async with
//  static member RunSynchronouslyEx(a:Async<'T>, timeout:int, cancellationToken) =
//    // Create cancellation token that is cancelled after 'timeout'
//    let timeoutCts = new CancellationTokenSource()
//    timeoutCts.CancelAfter(timeout)
//
//    // Create a combined token that is cancelled either when 
//    // 'cancellationToken' is cancelled, or after a timeout
//    let combinedCts = 
//      CancellationTokenSource.CreateLinkedTokenSource
//        (cancellationToken, timeoutCts.Token)
//
//    // Run synchronously with the combined token
//    try Async.RunSynchronously(a, cancellationToken = combinedCts.Token)
//    with :? OperationCanceledException as e ->
//      // If the timeout occurred, then we throw timeout exception instead
//      if timeoutCts.IsCancellationRequested then
//        raise (new System.TimeoutException())
//      else reraise()

