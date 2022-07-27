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

    /// <summary>Write a log item to the console.</summary>
    /// <param name="item">The PeepsLogItem to be written to the console.</param>
    /// <returns>Nothing.</returns>
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

    /// <summary>Write a PeepsLogItem to a LogStore.<summary>
    /// <param name="store">The LogStore to be written to.</summary>
    /// <param name="item">The PeepsLogItem to be written to the console.</param>
    /// <returns>Nothing.</returns>
    let writeToStore (store: LogStore) (item: PeepsLogItem) = store.AddItem item

    /// <summary>A message to be posted to a http endpoint.<summary>
    [<CLIMutable>]
    type Message =
        { [<JsonPropertyName("text")>]
          Text: string

          [<JsonPropertyName("from")>]
          From: string

          [<JsonPropertyName("type")>]
          Type: string

          [<JsonPropertyName("time")>]
          DateTime: DateTime }

    /// <summary> Post a PeepsLogItem to a http endpoint.</summary>
    /// <param name="client">A HttpClient to be used.</param>
    /// <param name="url">THe endpoint's url.</param>
    /// <param name="item">The PeepsLogItem to be written to the console.</param>
    /// <returns>Nothing.</returns>
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

            let content =
                JsonContent.Create(
                    { Text = item.Message
                      From = item.From
                      Type = t
                      DateTime = item.TimeUtc }
                )

            client.PostAsync(url, content)
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> ignore
        with
        | _ -> ()