namespace Peeps.Monitoring

open System.Net.Http
open System.Text.Json

module Tools =
    
    
    module HttpHelpers =
        
        let getString (url: string) (client: HttpClient) =
            try
                let response = client.GetAsync(url) |> Async.AwaitTask |> Async.RunSynchronously
                match response.IsSuccessStatusCode with
                | true -> Ok(response.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously)
                | false -> Error $"The server returned and error (code {response.StatusCode}): {response.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously}"
            with
            | ex -> Error $"Error trying to connect to the server: {ex.Message}"

        let get<'T> (url: string) (client: HttpClient) =
            match getString url client with
            | Ok r ->
                try
                    JsonSerializer.Deserialize<'T> r |> Ok
                with
                | ex -> Error $"Could not deserialize value: {r}"
            | Error e -> Error e
             
    type PeepsClient(http: HttpClient) =
       
        member _.GetBasicHealthCheck() = HttpHelpers.getString "/health" http
        
        member _.GetFullHealthCheck() = HttpHelpers.getString "/health/all" http
        
        member _.GetMetrics() = HttpHelpers.getString "/metrics" http