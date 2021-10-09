// Learn more about F# at http://fsharp.org

open System
open System.Diagnostics
open System.Diagnostics
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Diagnostics.HealthChecks
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Peeps
open Peeps.Core
open Peeps.Extensions
open Peeps.Monitoring
open Peeps.Monitoring
open Peeps.Monitoring.HealthChecks
open Peeps.Logger
open Giraffe.Middleware
open Peeps.Sqlite
open Peeps.Sqlite
open Peeps.Store

[<RequireQualifiedAccess>]
module Routes =

    let info: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("Test")
            use scope = logger.BeginScope("test", "")
            let corrRef = ctx.Items.["corr_ref"] :?> Guid


            logger.LogInformation "Hello, from info"
            logger.LogInformation $"Correlation ref: {corrRef}"

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
            
    let fail: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) -> failwith "Unhandled exception."


let webApp =
    choose [ route "/" >=> text "Hello, World!"
             route "/info" >=> Routes.info
             route "/debug" >=> Routes.debug
             route "/error" >=> Routes.error
             route "/warning" >=> Routes.warn
             route "/fail" >=> Routes.fail
             route "/metrics" >=> Routes.metrics ]

let configureApp (app: IApplicationBuilder) =
    
    //if env.IsDevelopment() then
    //    app.UseDeveloperExceptionPage() |> ignore
    //env.ConfigureAppConfiguration(fun ctx cfg -> ctx.HostingEnvironment)
    
    app.UsePeepsMonitor()
       .UsePeepsLiveView()
       .UseRouting()
       .UsePeepsHealthChecks()
       .UseGiraffe webApp

let configureServices startedOn (services: IServiceCollection) =
    services
        //.UseGiraffeErrorHandler(errorHandler)
        .AddPeepsMonitorAgent("C:\\ProjectData\\WSTest")
        .AddGiraffe() |> ignore
    
    services.AddHealthChecks()
            .AddPeepsHealthChecks(5000000L, 1000, startedOn)
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

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseKestrel()
                    .UseUrls("http://localhost:20999;https://localhost:21000;")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices startedOn)
                    .ConfigureLogging(configureLogging peepsCtx)
                |> ignore)
            .Build()
            .Run()

        0 // return an integer exit code
    | None ->
        printfn "Missing path arg."
        -1
    
