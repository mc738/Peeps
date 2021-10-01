namespace Peeps.LiveView

open System
open System.Net.WebSockets
open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

module Middleware =
    
    let mutable sockets = list<WebSocket>.Empty
    
    let private addSocket sockets socket = socket :: sockets
    
    let private removeSocket sockets socket =
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
    
    type WebSocketMiddleware(next: RequestDelegate) =
        member _.Invoke(ctx: HttpContext) =
            async {
                if ctx.Request.Path = PathString("/log") then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        //let logger =ctx.GetLogger("ws-middleware")
                        //logger.LogInformation("Connection received.")
                        use! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                        sockets <- addSocket sockets webSocket
                                    
                        printfn $"Socket state: {webSocket.State}"
                        let buffer: byte array = Array.zeroCreate 4096
                        //do! Async.Sleep 5000
                        
                        let! ct = Async.CancellationToken
                        //let! _ = sendMessage webSocket "Hello client"

                        while true do                   
                            //printfn $"{webSocket.State} {webSocket.CloseStatusDescription}"
                            //webSocket.ReceiveAsync(ArraySegment<byte>(buffer), ct)
                            //|> Async.AwaitTask
                            //|> ignore
                            do! Async.Sleep 1000
                            
                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke(ctx) |> Async.AwaitTask
            } |> Async.StartAsTask :> Task