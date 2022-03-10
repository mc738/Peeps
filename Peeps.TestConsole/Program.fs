// Learn more about F# at http://fsharp.org

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

let createHostBuilder (args) =
    Host
        .CreateDefaultBuilder(args)
        .ConfigureLogging(fun logging ->
            logging.ClearProviders() |> ignore)
            //logging.AddPeeps() |> ignore)

[<EntryPoint>]
let main argv =
    let host = createHostBuilder argv

    host.Build().Run()
      
    printfn "Hello World from F#!"
    0 // return an integer exit code