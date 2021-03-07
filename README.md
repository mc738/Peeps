# Peeps

`Peeps` is a simple logger in the written in `F#`.

It is named after `Samual Peeps`, the idea is it will log what your application is doing as it burns.

# Set up 

## Giraffe
 
1. Add imports:
```fsharp
open Peeps
open Peeps.Extensions
open Peeps.PeepsLogger
open Peeps.Sqlite
```

2. Create a `PeepsContext`
```fsharp
let peepsCtx = PeepsContext.Create("/home/max/Data/logs", "Test")
```

3. Configure logging:
```fsharp
let configureLogging (peepsCtx: PeepsContext) (logging: ILoggingBuilder) =
logging.ClearProviders() |> ignore
logging.AddPeeps(peepsCtx) |> ignore
```

4. Add logging to `ASP.Net core` requst pipeline:
```fsharp
Host
    .CreateDefaultBuilder()
    .ConfigureWebHostDefaults(fun webHostBuilder ->
        webHostBuilder
            .UseKestrel()
            .Configure(configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging peepsCtx)
        |> ignore)
    .Build()
    .Run()
```