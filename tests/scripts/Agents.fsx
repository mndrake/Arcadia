


let inbox = MailboxProcessor.Start(fun agent -> 
 
// Function that implements the body of the agent
let rec loop sum count = async {
  // Asynchronously wait for the next message
  let! msg = agent.Receive()
  match msg with
  | Reset -> 
      // Restart loop with initial values
      return! loop 0.0 0.0
 
  | Update value -> 
      // Update the state and print the statistics
      let sum, count = sum + value, count + 1.0
      printfn "Average: %f" (sum / count)
 
      // Wait before handling the next message
      do! Async.Sleep(1000)
      return! loop sum count }
 
// Start the body with initial values
loop 0.0 0.0)