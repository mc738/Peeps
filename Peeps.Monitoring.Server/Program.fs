// Learn more about F# at http://fsharp.org

open System
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Peeps
open Peeps.Core
open Peeps.Extensions
open Peeps.Monitoring
open Peeps.Logger
open Peeps.Sqlite

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

let configureServices (services: IServiceCollection) =
    services
        //.UseGiraffeErrorHandler(errorHandler)
        .AddPeepsMonitorAgent("C:\\ProjectData\\WSTest")
        .AddGiraffe() |> ignore
    
    services.AddHealthChecks()
            .AddPeepsHealthChecks(5000000L, 1000, DateTime.UtcNow)
            |> ignore

let configureLogging (peepsCtx: PeepsContext) (logging: ILoggingBuilder) =
    logging.ClearProviders() |> ignore
    logging.AddPeeps(peepsCtx) |> ignore


type ServiceDetails = {
    Name: string
    Url: string
    HasHealthChecks: bool
    HasMetrics: bool
}

type ServerConfiguration = {
    Urls: string
    DataPath: string
}

[<EntryPoint>]
let main argv =

    let liveView (item: PeepsLogItem) =
        let t =
            match item.ItemType with
            | LogItemType.Information -> "info"
            | LogItemType.Debug -> "debug"
            | LogItemType.Trace -> "trace"
            | LogItemType.Error -> "error"
            | LogItemType.Warning -> "warning"
            | LogItemType.Critical -> "critical"

        let message =
            ({ Text = item.Message
               From = item.From
               Type = t
               DateTime = item.TimeUtc }: Actions.Message)

        LiveView.sendMessageToSockets (System.Text.Json.JsonSerializer.Serialize message)
        |> Async.RunSynchronously

    let dbWriter =
        DbWriter("C:\\ProjectData\\WSTest\\logs", "peeps-test")

    use client = new HttpClient()

    let actions =
        [ Actions.writeToConsole
          Actions.writeToDb dbWriter
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
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging peepsCtx)
            |> ignore)
        .Build()
        .Run()

    0 // return an integer exit code