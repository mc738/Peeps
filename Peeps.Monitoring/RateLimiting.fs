namespace Peeps.Monitoring

open System

module RateLimiting =
    
    module Internal =
        
        type Request =
            { Key: string
              ReplyChannel: AsyncReplyChannel<bool> }

        type State =
            { PreviousWindow: Map<string, int>
              CurrentWindow: Map<string, int> }

        let checkRate (previousCount: int) (currentCount: int) (second: int) (limit: int) =
            (int (float previousCount * (60. - float second)/ 60.) + currentCount) < limit

        let startAgent limit =
            MailboxProcessor<Request>.Start
                (fun inbox ->
                    let rec loop (window: DateTime, state: State) =
                        async {

                            let now = DateTime.UtcNow
                            let! request = inbox.Receive()
         
                                 
                            let newState =
                                match window.Minute = now.Minute with
                                | true ->
                                    // Same minute. do not need a new window.
                                    let count =
                                        state.CurrentWindow.TryFind request.Key
                                        |> Option.defaultValue 0
                                    { state with
                                          CurrentWindow = state.CurrentWindow.Add(request.Key, count + 1) }
                                
                                | false ->
                                    printfn "========= New window"
                                    printfn $"{state}"
                                    // Make a new window
                                    { state with
                                                  PreviousWindow = state.CurrentWindow
                                                  CurrentWindow = [ request.Key, 1 ] |> Map.ofList }
                                    
                            let prevCount =
                                state.PreviousWindow.TryFind request.Key
                                |> Option.defaultValue 0        
                                    
                            let currCount =
                                state.CurrentWindow.TryFind request.Key
                                |> Option.defaultValue 0
                               
                            request.ReplyChannel.Reply(checkRate prevCount currCount now.Second limit)
                            
                            return! loop(now, newState)
                        }



                    loop (
                        DateTime.UtcNow,
                        { PreviousWindow = Map.empty
                          CurrentWindow = Map.empty }
                    ))


    type RateLimitingAgent(limit) =
        let agent = Internal.startAgent(limit)
        
        member rla.InLimit(key) =
            agent.PostAndReply(fun rc -> ({ Key = key; ReplyChannel = rc }: Internal.Request))
