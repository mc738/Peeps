# Peeps

`Peeps` is a logger and set of monitoring tools in the written in `F#`.

It is named after `Samual Peeps`, the idea is it will log what your application is doing as it burns.

**NOTE** I now realise his name was `Samuel Pepys`.

The are 3 main projects:

* `Peeps` - The core logging library and `ILogger` implementation.
* `Peeps.Monitoring` - In-app Monitoring tools
  * LiveView - A live view of application logs served over `websockets`.
  * HealthChecks - A collection of standardized health checks.
  * Monitoring agent - A monitoring agent that can be wrapped around requests to handled uncaught errors and collect metrics.
* `Peeps.Tools` - A collection of tools for working with peeps data and end points. 

## LogStore

`Store.fs` contains a `Sqlite` backed store for log items.
This uses an agent so should be a singleton.

Set `Set up` section for examples of using the log store.

The store will create a database with the naming convention `"{name}-{runId:N}.log"`,
in a `[APPDATA]/log` directory. 

Also the file `.peeps_lock` will be created/overwritten with the name of the current lock.

## Actions

Actions represent what the logger will do with items it receives.

They all share the same signature:

`PeepsLogItem -> unit`

The core library comes with 3:

* `writeToConsole`
* `writeToStore [LogStore]`
* `httpPost [HttpClient] [url]`

An example of implementing a write to file log action:

```fsharp
let writeToFile (path: string) (item: PeepsLogItem) = File.AppendAllText(path, $"{item}{Environment.NewLine}")
```

`Peeps.Monitor` (will) contain:

* `toLiveView` (TODO - not implemented yet)
  * Current implement (from `Peeps.TestWebApi`):
```fsharp
let liveView (item: PeepsLogItem) =
    let message =
        ({ Text = item.Message
           From = item.From
           Type = item.ItemType.Serialize()
           DateTime = item.TimeUtc }: Actions.Message)

    LiveView.sendMessageToSockets (System.Text.Json.JsonSerializer.Serialize message)
    |> Async.RunSynchronously
```

# Set up 

The main logger can be used anywhere `ILogger` would be used. There are version extensions for convince.

The best current example of setting up Peeps with `giraffe` can be found in the `Peeps.TestWebApi` project.

## Configuring application
```fsharp
let configureApp (app: IApplicationBuilder) =
    
    // `UsePeepsMonitor` added first to wrap whole request pipeline.
    // So it can capture metrics and handle unhandled expections (and report them).
    app.UsePeepsMonitor()
       // Adds live view (via websockets).
       .UsePeepsLiveView()
       .UseRouting()
       // Sets up standardized health check end points.
       .UsePeepsHealthChecks()
       // Configure rest of application.
```
*Example version `v-0.3.0`*

## Configuring services
```fsharp
let configureServices (store: LogStore) (services: IServiceCollection) =
  services
      // Adds the LogStore as singleton service. 
      .AddPeepsLogStore(store)
      // Used for storing metrics etc.
      .AddPeepsMonitorAgent("APP_DATA_PATH")
      // Configure other services...
      |> ignore
  
  services.AddHealthChecks()
          // Add peeps health check with max allocation of 5000000 bytes and 1000 minute run time.
          // Will return unhealthy if either reached.
          .AddPeepsHealthChecks(5000000L, 1000, store.StartedOn)
          // Configure heath checks...
          |> ignore
```
*Example version `v-0.3.0`*

## Configure logging
```fsharp
let configureLogging (peepsCtx: PeepsContext) (logging: ILoggingBuilder) =
    logging.ClearProviders() |> ignore
    logging.AddPeeps(peepsCtx) |> ignore
```
*Example version `v-0.3.0`*

## Build application
```fsharp
let startedOn = DateTime.UtcNow
let runId = Guid.NewGuid()

// Current in-place implementation for LiveView log action.
let liveView (item: PeepsLogItem) =
    let message =
        ({ Text = item.Message
           From = item.From
           Type = item.ItemType.Serialize()
           DateTime = item.TimeUtc }: Actions.Message)

    LiveView.sendMessageToSockets (System.Text.Json.JsonSerializer.Serialize message)
    |> Async.RunSynchronously

let logStore = LogStore(path, "test_api", runId, startedOn)

use client = new HttpClient()

let actions =
    [ Actions.writeToConsole
      Actions.writeToStore logStore
      liveView
      //Actions.httpPost client "http://localhost:5000/message"
      ]

// Set up the Peeps context.
let peepsCtx =
    PeepsContext.Create(AppContext.BaseDirectory, "Test", actions)

// This is for a web app, but similar princples would apply for other app types.
Host
    .CreateDefaultBuilder()
    .ConfigureWebHostDefaults(fun webHostBuilder ->
        webHostBuilder
            .UseKestrel()
            .UseUrls("http://localhost:20999;https://localhost:21000;")
            .Configure(configureApp)
            .ConfigureServices(configureServices logStore)
            .ConfigureLogging(configureLogging peepsCtx)
        |> ignore)
    .Build()
    .Run()
```
*Example version `v-0.3.0`*
