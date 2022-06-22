namespace Peeps

open System
open System
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open Peeps.Core
open Peeps.Store

[<RequireQualifiedAccess>]
module Actions =
    
    let writeToConsole (item: PeepsLogItem) =
        let color =
            match item.ItemType with
            | Information -> ConsoleColor.DarkGray
            | Debug -> ConsoleColor.Magenta
            | Trace -> ConsoleColor.White
            | Error -> ConsoleColor.DarkRed
            | Warning -> ConsoleColor.Yellow
            | Critical -> ConsoleColor.DarkRed
        
        Console.ForegroundColor <- color
        printfn $"{item.Message}"
        Console.ResetColor()

    let writeToStore (store: LogStore) (item: PeepsLogItem) = store.AddItem item
        
    [<CLIMutable>]
    type Message ={
        [<JsonPropertyName("text")>]
        Text: string
        
        [<JsonPropertyName("from")>]
        From: string
        
        [<JsonPropertyName("type")>]
        Type: string
        
        [<JsonPropertyName("time")>]
        DateTime: DateTime
    }
    
    let httpPost (client: HttpClient) (url: string) (item: PeepsLogItem) =
        try
            let t =
                match item.ItemType with
                | LogItemType.Information -> "info"
                | LogItemType.Debug -> "debug"
                | LogItemType.Trace -> "trace"
                | LogItemType.Error -> "error"
                | LogItemType.Warning -> "warning"
                | LogItemType.Critical -> "critical"
            
            let content = JsonContent.Create({ Text = item.Message; From = item.From; Type = t; DateTime = item.TimeUtc })
            client.PostAsync(url, content) |> Async.AwaitTask |> Async.RunSynchronously |> ignore 
        with
        | _ -> ()
        
    
    