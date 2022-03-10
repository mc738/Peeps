namespace Peeps.Monitoring

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Peeps.Store


module PeepsMetricRoutes =
    
    let requestMetrics: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("request-metrics")

            logger.LogInformation "Hello, from metrics"
            let service = ctx.GetService<PeepsMonitorAgent>()
            let metrics = service.GetMetrics()
            json metrics next ctx
            
    let logMetrics: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let logger = ctx.GetLogger("log-metrics")

            logger.LogInformation "Hello, from log count"
            let service = ctx.GetService<LogStore>()
            let itemCount = service.ItemCount() |> string
            text itemCount next ctx
            
    let routes: (HttpFunc -> HttpContext -> HttpFuncResult) list =
        [ GET
        >=> choose [ route "/metrics/requests/" >=> warbler (fun _ -> requestMetrics)
                     route "/metrics/log/" >=> warbler (fun _ -> logMetrics) ] ]