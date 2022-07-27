// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Net.Http
open Freql.MySql
open Freql.Sqlite
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Peeps


open Peeps.Extensions
open Peeps.Monitoring
open Peeps.Logger
open Peeps.Monitoring.DataStores
open Peeps.Monitoring.Metrics
open Peeps.Store

[<RequireQualifiedAccess>]
module Routes =

    let info: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")
            use scope = logger.BeginScope("test", "")
            let corrRef = ctx.Items.["corr_ref"] :?> Guid
            let ip = ctx.Items.["ip_address"]


            logger.LogInformation "Hello, from info"
            logger.LogInformation $"Correlation ref: {corrRef}"
            logger.LogInformation $"IP: {ip}"

            text "Info" next ctx

    let debug: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogDebug "Hello, from debug"

            text "Debug" next ctx

    let error: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogError "Hello, from error"

            text "Error" next ctx

    let warn: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogWarning "Hello, from warning"

            text "Warn" next ctx
                
    let metrics: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogInformation "Hello, from metrics"
            let service = ctx.GetService<PeepsMonitorAgent>()
            let metrics = service.GetMetrics()
            json metrics next ctx
            
    let logCount: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("log_count")

            logger.LogInformation "Hello, from log count"
            let service = ctx.GetService<LogStore>()
            let itemCount = service.ItemCount() |> string
            text itemCount next ctx
            
    let logConnection: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("log_connection")

            logger.LogInformation "Hello, from log connect"
            let service = ctx.GetService<LogStore>()
            //let r =
            match service.CheckConnection() with
            | Ok _ -> 
                text "Connection is ok" next ctx
            | Result.Error e ->
                text $"Log connection error: {e}" next ctx
                
    let fail: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) -> failwith "Unhandled exception."

let webApp =
    choose [ route "/" >=> text "Hello, World!"
             route "/info" >=> Routes.info
             route "/debug" >=> Routes.debug
             route "/error" >=> Routes.error
             route "/warning" >=> Routes.warn
             route "/fail" >=> Routes.fail
             route "/metrics" >=> Routes.metrics
             route "/log/count" >=> Routes.logCount
             route "/log/connection" >=> Routes.logConnection ]

let configureApp (app: IApplicationBuilder) =
    
    //if env.IsDevelopment() then
    //    app.UseDeveloperExceptionPage() |> ignore
    //env.ConfigureAppConfiguration(fun ctx cfg -> ctx.HostingEnvironment)
    
    app.UsePeepsMonitor()
       .UsePeepsLiveView()
       .UseRouting()
       .UsePeepsHealthChecks()
       .UseGiraffe webApp


//let saveErrorToFile (response: ResponsePost) (ex: exn) =
//    File.WriteAllText($"C:\\ProjectData\\Peeps\\errors\\{DateTime.UtcNow:yyyyMMddHHmmss}_{response.CorrelationReference}.error", ex.ToString())

//let criticalHandlers = [
//    saveErrorToFile
//]

let configureServices (store: LogStore) (metricsCfg: MonitoringStoreConfiguration) (services: IServiceCollection) =
    services
        //.UseGiraffeErrorHandler(errorHandler)
        .AddPeepsLogStore(store)
        .AddPeepsMonitorAgent(metricsCfg)
        //.AddScoped()
        //.AddMySqlLogStore()
        .AddPeepsRateLimiting(10)
        .AddGiraffe() |> ignore
    
    services.AddHealthChecks()
            .AddPeepsHealthChecks(5000000L, 1000, store.StartedOn)
            |> ignore

let configureLogging (peepsCtx: PeepsContext) (logging: ILoggingBuilder) =
    logging.ClearProviders() |> ignore
    logging.AddPeeps(peepsCtx) |> ignore

[<EntryPoint>]
let main argv =

    let pathArg =
        match argv.Length > 0 with
        | true -> Some argv.[0]
        | false -> None
        
    match pathArg with
    | Some path ->
        let startedOn = DateTime.UtcNow
        let runId = Guid.NewGuid()
        
        let logStore = LogStore(path, "test_api", runId, startedOn)
        
        // Sqlite
        //let metricsStore = SqliteContext.Create(Path.Combine(path, $"test_api-metrics-{runId}.db"))
        
        use client = new HttpClient()

        let cs = Environment.GetEnvironmentVariable("PEEPS_LOGGING_CONNECTION_STRING")
    
        let actions =
            [ Actions.writeToConsole
              Actions.writeToStore logStore
              LiveView.logAction
              DataStores.MySql.LogStore.action cs
              //Actions.httpPost client "http://localhost:5000/message"
              ]

        // Sqlite metric store
        //let monitoringCfg = Monitoring.DataStores.Sqlite.Store.config metricsStore
        
        let monitoringCfg = Monitoring.DataStores.MySql.Store.config cs
        
        // Set up the Peeps context.
        let peepsCtx =
            PeepsContext.Create(AppContext.BaseDirectory, "Test", actions)

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseKestrel()
                    .UseUrls("http://0.0.0.0:20999;https://0.0.0.0:21000;")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices logStore monitoringCfg)
                    .ConfigureLogging(configureLogging peepsCtx)
                |> ignore)
            .Build()
            .Run()

        0 // return an integer exit code
    | None ->
        printfn "Missing path arg."
        -1