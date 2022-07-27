namespace Peeps.Monitoring

module Middleware =

    open System
    open System.Diagnostics
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Http
    open Giraffe
    open Microsoft.Extensions.Logging
    open Peeps.Monitoring.Metrics
    open Peeps.Monitoring.RateLimiting
    
    type PeepsMonitorMiddleware(next: RequestDelegate) =

        member _.Invoke(ctx: HttpContext) =
            async {
                //let logger = ctx.GetLogger("request-tracker")
                let stopwatch = Stopwatch()
                stopwatch.Start()
                let corrRef = Guid.NewGuid()
                let ma = ctx.GetService<PeepsMonitorAgent>()
                let rla = ctx.GetService<RateLimitingAgent>()

                let ipAddress =
                    ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString()

                match rla.InLimit(ipAddress) with
                | true ->
                    ma.SaveRequest(
                        corrRef,
                        ipAddress,
                        ctx.Request.ContentLength
                        |> Option.ofNullable
                        |> Option.defaultValue 0L,
                        ctx.GetRequestUrl()
                    )

                    ctx.Items.Add("corr_ref", corrRef)
                    ctx.Items.Add("ip_address", ipAddress)

                    try
                        do! next.Invoke(ctx) |> Async.AwaitTask
                        stopwatch.Stop()

                        let outcome =
                            match ctx.Response.StatusCode < 400 with
                            | true -> ma.SaveResponse
                            | false -> ma.SaveError

                        outcome (
                            corrRef,
                            ctx.Response.ContentLength
                            |> Option.ofNullable
                            |> Option.defaultValue 0L,
                            ctx.Response.StatusCode,
                            stopwatch.ElapsedMilliseconds
                        )

                    with
                    | ex ->
                        stopwatch.Stop()
                        let logger = ctx.GetLogger("peeps-monitor")
                        logger.LogCritical($"Unhandled exception in route '{ctx.GetRequestUrl()}'. Error: {ex.Message}")
                        ctx.Response.StatusCode <- 500

                        ma.SaveCritical(
                            corrRef,
                            ctx.Response.ContentLength
                            |> Option.ofNullable
                            |> Option.defaultValue 0L,
                            ctx.Response.StatusCode,
                            stopwatch.ElapsedMilliseconds,
                            ex
                        )
                | false -> ctx.Response.StatusCode <- 429
            }
            |> Async.StartAsTask
            :> Task

    type PeepsLiveViewMiddleware(next: RequestDelegate) =
        member _.Invoke(ctx: HttpContext) =
            async {
                if ctx.Request.Path = PathString("/log/live") then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        use! webSocket =
                            ctx.WebSockets.AcceptWebSocketAsync()
                            |> Async.AwaitTask

                        LiveView.sockets <- LiveView.addSocket LiveView.sockets webSocket
                        printfn $"Socket state: {webSocket.State}"
                        let buffer: byte array = Array.zeroCreate 4096
                        //do! Async.Sleep 5000
                        let! ct = Async.CancellationToken

                        while true do
                            // Needed?
                            do! Async.Sleep 1000

                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke(ctx) |> Async.AwaitTask
            }
            |> Async.StartAsTask
            :> Task
