namespace Peeps.Monitoring

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Diagnostics.HealthChecks
open Peeps.Monitoring.HealthChecks
open Peeps.Monitoring.Middleware

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
        
    type IServiceCollection with
        
        member builder.AddPeepsMonitorAgent(path) =
            builder.AddSingleton<PeepsMonitorAgent>(fun _ -> PeepsMonitorAgent(path))
    

