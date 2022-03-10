namespace Peeps.Monitoring

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Diagnostics.HealthChecks
open Peeps.Monitoring.HealthChecks
open Peeps.Monitoring.Middleware
open Peeps.Monitoring.RateLimiting
open Peeps.Store

[<AutoOpen>]
module Extensions =
    
    type IHealthChecksBuilder with
        
        member builder.AddPeepsHealthChecks(maxSize: int64, maxRunTimeMinutes: int, startTime: DateTime) =
            builder
                .AddTypeActivatedCheck<PeepsMemoryHealthCheck>(
                    PeepsMemoryHealthCheck.Name,
                    PeepsMemoryHealthCheck.FailureStatus,
                    PeepsMemoryHealthCheck.Tags,
                    args = [| maxSize |])
                .AddTypeActivatedCheck<PeepsUptimeHealthCheck>(
                    PeepsUptimeHealthCheck.Name,
                    PeepsUptimeHealthCheck.FailureStatus,
                    PeepsUptimeHealthCheck.Tags,
                    args = [| maxRunTimeMinutes; startTime |])
            
    type IApplicationBuilder with
        
        member builder.UsePeepsHealthChecks() =
            builder.UseEndpoints(fun ep ->
                let basicOptions = HealthCheckOptions()
                basicOptions.Predicate <- fun hc -> hc.Tags.Contains("basic")
                // Apparently needs to be wrapped in an anonymous function.
                basicOptions.ResponseWriter <- fun c r -> ResponseWriter.handler c r
                ep.MapHealthChecks("/health", basicOptions) |> ignore
                let fullOptions = HealthCheckOptions()
                // Apparently needs to be wrapped in an anonymous function.
                fullOptions.ResponseWriter <- fun c r -> ResponseWriter.handler c r
                ep.MapHealthChecks("/health/full", fullOptions) |> ignore)
        
        member builder.UsePeepsMonitor() = builder.UseMiddleware<PeepsMonitorMiddleware>()
        
        member builder.UsePeepsLiveView() =
            builder
                .UseWebSockets()
                .UseMiddleware<PeepsLiveViewMiddleware>()
        
    type IServiceCollection with
        
        member builder.AddPeepsLogStore(store: LogStore) =
            builder.AddSingleton<LogStore>(store)
        
        /// Use peeps monitoring agent with default critical error handler(s).
        /// Currently that includes save a copy of the error to the path provided.
        /// These take the form are [dateTime]_[corrId].error
        member builder.AddPeepsMonitorAgent(path) =
            let saveErrorToFile (response: ResponsePost) (ex: exn) =
                File.WriteAllText(Path.Combine(path, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{response.CorrelationReference}.error"), ex.ToString())
            
            builder.AddSingleton<PeepsMonitorAgent>(fun _ -> PeepsMonitorAgent(path, [ saveErrorToFile ]))
            
        /// Use peeps monitoring agent with a bespoke set of critical handlers.
        member builder.AddPeepsMonitorAgent(path, criticalHandlers) =
            builder.AddSingleton<PeepsMonitorAgent>(fun _ -> PeepsMonitorAgent(path, criticalHandlers))
            
        member builder.AddPeepsRateLimiting(limit) =
            builder.AddSingleton<RateLimitingAgent>(fun _ -> RateLimitingAgent(limit))