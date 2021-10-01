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
                let ma = ctx.GetService<PeepsMonitorAgent>()

                ma.SaveRequest(
                    corrRef,
                    ctx.Request.ContentLength
                    |> Option.ofNullable
                    |> Option.defaultValue 0L,
                    ctx.GetRequestUrl()
                )

                ctx.Items.Add("corr_ref", corrRef)
                do! next.Invoke(ctx) |> Async.AwaitTask
                stopwatch.Stop()

                ma.SaveResponse(
                    corrRef,
                    ctx.Response.ContentLength
                    |> Option.ofNullable
                    |> Option.defaultValue 0L,
                    ctx.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                )

                printfn
                    $"*** Correlation request {corrRef} completed. Response size: {ctx.Response.ContentLength}. Time (ms): {stopwatch.ElapsedMilliseconds}."
            }
            |> Async.StartAsTask
            :> Task
