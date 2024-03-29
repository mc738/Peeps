﻿namespace Peeps.Monitoring

[<AutoOpen>]
module Extensions =

    open System
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Diagnostics.HealthChecks
    open Microsoft.Extensions.DependencyInjection
    open Peeps.Monitoring.DataStores.MySql.Store
    open Peeps.Monitoring.HealthChecks
    open Peeps.Monitoring.Metrics
    open Peeps.Monitoring.Middleware
    open Peeps.Monitoring.RateLimiting
    open Peeps.Store
        
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
        
        /// Use peeps monitoring agent with a bespoke set of critical handlers.
        member builder.AddPeepsMonitorAgent(cfg) =
            builder.AddSingleton<PeepsMonitorAgent>(fun _ -> PeepsMonitorAgent(cfg))
            
        member builder.AddPeepsRateLimiting(limit) =
            builder.AddSingleton<RateLimitingAgent>(fun _ -> RateLimitingAgent(limit))
            
        member builder.AddMySqlLogStore() =
            builder.AddScoped<MySqlLogStore>()