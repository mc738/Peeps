namespace Peeps.Monitoring

open System
open System.Diagnostics
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe

module Middleware =
    
    type PeepsMonitorMiddleware(next: RequestDelegate) =
        
        member _.Invoke(ctx: HttpContext) =
            async {
                //let logger = ctx.GetLogger("request-tracker")
                let stopwatch = Stopwatch()
                stopwatch.Start()
                let corrRef = Guid.NewGuid()
                //printfn($"Correlation request {corrRef}. Route: {ctx.GetRequestUrl()}. Size: {ctx.Request.ContentLength}")
                printfn $"*** Correlation request {corrRef}. Route: {ctx.GetRequestUrl()}. Size: {ctx.Request.ContentLength}"
                //logger.LogInformation()
                ctx.Items.Add("corr_ref", corrRef)
                do! next.Invoke(ctx) |> Async.AwaitTask
                stopwatch.Stop()
                printfn $"*** Correlation request {corrRef} completed. Response size: {ctx.Response.ContentLength}. Time (ms): {stopwatch.ElapsedMilliseconds}."
            } |> Async.StartAsTask :> Task

