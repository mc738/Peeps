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
open Peeps.Extensions
open Peeps.Sqlite

[<RequireQualifiedAccess>]
module Routes =
    
    let info: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogInformation "Hello, from info"            
            
            text "Info" next ctx
    
    
    let debug: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogDebug "Hello, from debug"            
            
            text "Debug" next ctx
            
    
    let error: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogError "Hello, from error"            
            
            text "Error" next ctx
            
    
    let warn: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("Test")

            logger.LogWarning "Hello, from warning"            
            
            text "Warn" next ctx
            

let webApp =
    choose [
        route "/" >=> text "Hello, World!"
        route "/info" >=> Routes.info
        route "/debug" >=> Routes.debug
        route "/error" >=> Routes.error
        route "/warning" >=> Routes.warn       
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp
    
let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

let configureLogging (dbWriter: DbWriter) (logging : ILoggingBuilder) =
    logging.ClearProviders() |> ignore
    logging.AddPeeps(dbWriter) |> ignore
    
[<EntryPoint>]
let main argv =
    
    
    
    // Set up dbwriter.
    let dbWriter = DbWriter("/home/max/Data/logs", "Test")
    
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseKestrel()
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging dbWriter)
                    |> ignore)
        .Build()
        .Run()
    // Close the peeps context.    
    dbWriter.Close()

    0 // return an integer exit code
