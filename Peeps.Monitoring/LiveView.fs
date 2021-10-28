namespace Peeps.Monitoring

open System
open System.Net.WebSockets
open System.Text
open System.Threading
open Peeps
open Peeps.Core

module LiveView =
    let mutable sockets = list<WebSocket>.Empty
    
    let addSocket sockets socket = socket :: sockets
    
    let removeSocket sockets socket =
        sockets
        |> List.choose (fun s -> if s <> socket then Some s else printfn "Removing socket"; None)
        
    let private sendMessage =
        fun (socket: WebSocket) (message: string) -> async {
            let buffer = Encoding.UTF8.GetBytes message
            let segment = ArraySegment<byte>(buffer)
            
            if socket.State = WebSocketState.Open then
                do! socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                    |> Async.AwaitTask
            else
                sockets <- removeSocket sockets socket
        }
        
    let sendMessageToSockets =
        fun message ->
            async {
                for socket in sockets do
                    try
                        do! sendMessage socket message
                    with
                        | ex -> printfn $"{socket.State} {ex.Message}"; sockets <- removeSocket sockets socket
            }
            
    let logAction (item: PeepsLogItem) =
        let message =
                ({ Text = item.Message
                   From = item.From
                   Type = item.ItemType.Serialize()
                   DateTime = item.TimeUtc }: Actions.Message)

        sendMessageToSockets (System.Text.Json.JsonSerializer.Serialize message)
        |> Async.RunSynchronously