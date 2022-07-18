namespace Peeps.Monitoring

open System
open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Diagnostics.HealthChecks

/// <summary>Standardized Peeps health checks for us in ASP.NET applications</summary>
module HealthChecks =

    [<RequireQualifiedAccess>]
    module ResponseWriter =

        let handler (context: HttpContext) (report: HealthReport) =
            context.Response.ContentType <- "application/json; charset=utf-8"

            let mutable options = JsonWriterOptions()
            options.Indented <- true

            use ms = new MemoryStream()
            use writer = new Utf8JsonWriter(ms, options)

            writer.WriteStartObject()
            writer.WriteString("status", report.Status.ToString())
            writer.WriteStartObject("results")

            report.Entries
            |> List.ofSeq
            |> List.map
                (fun e ->
                    writer.WriteStartObject(e.Key)
                    writer.WriteString("status", e.Value.Status.ToString())
                    writer.WriteString("description", e.Value.Description)
                    writer.WriteStartObject("data")

                    e.Value.Data
                    |> List.ofSeq
                    |> List.map
                        (fun d ->
                            writer.WritePropertyName(d.Key)
                            JsonSerializer.Serialize(writer, d.Value))
                    |> ignore

                    writer.WriteEndObject()
                    writer.WriteEndObject())
            |> ignore

            writer.WriteEndObject()
            writer.WriteEndObject()
            writer.Flush()

            let json = Encoding.UTF8.GetString(ms.ToArray())
            context.Response.WriteAsync(json)

    /// <summary>Health check to monitor the amount of memory an application is using.</summary>
    type PeepsMemoryHealthCheck(total) =

        interface IHealthCheck with

            /// <summary>Run the health check.</summary>
            /// <param name="context">The HealthCheckContext.</param>
            /// <param name="cancellationToken">A cancellation token.</param>
            /// <returns>A task that represents the asynchronous HealthCheckResult.</returns>
            member this.CheckHealthAsync(context, cancellationToken) =
                let allocated = GC.GetTotalMemory(false)
                let totalAllocated = GC.GetTotalAllocatedBytes(false)

                let data =
                    [ "allocatedBytes", box allocated
                      "totalAllocated", box totalAllocated
                      "gen0Collection", box (GC.CollectionCount(0))
                      "gen1Collection", box (GC.CollectionCount(1))
                      "gen2Collection", box (GC.CollectionCount(2))
                      "gen3Collection", box (GC.CollectionCount(3)) ]
                    |> Map.ofList

                let status =
                    match allocated < total with
                    | true -> HealthStatus.Healthy
                    | false -> context.Registration.FailureStatus

                let desc =
                    $"Reports degraded status if allocated bytes when allocated bytes is above {total}."

                Task.FromResult(HealthCheckResult(status, desc, data = data))
             
        /// <summary>The health check's tags</summary>
        /// <returns>A array of string tags for the health check.</returns>
        static member Tags = [| "diagnostic"; "memory"; "internal"; "full" |]
        
        /// <summary>The health check's name.</summary>
        /// <returns>A array of string tags for the health check.</returns>
        static member Name = "memory"
        
        /// <summary>The health check's failure status.</summary>
        /// <returns>HealthStatus.Unhealthy</returns>
        static member FailureStatus = HealthStatus.Unhealthy

    /// <summary>Health check to monitor how long an application has been running.</summary>
    type PeepsUptimeHealthCheck(maxRunTime, startTime: DateTime) =

        interface IHealthCheck with
        
            /// <summary>Run the health check.</summary>
            /// <param name="context">The HealthCheckContext.</param>
            /// <param name="cancellationToken">A cancellation token.</param>
            /// <returns>A task that represents the asynchronous HealthCheckResult.</returns>
            member this.CheckHealthAsync(context, cancellationToken) =

                let uptime = (DateTime.UtcNow - startTime).Minutes

                let data =
                    [ "startedOn", box startTime
                      "uptime", box (DateTime.UtcNow - startTime).Minutes ]
                    |> Map.ofList

                let status =
                    match uptime < maxRunTime with
                    | true -> HealthStatus.Healthy
                    | false -> context.Registration.FailureStatus

                let desc =
                    $"Reports degraded status if run time minutes is above {maxRunTime}."

                Task.FromResult(HealthCheckResult(status, desc, data = data))

        /// <summary>The health check's tags</summary>
        /// <returns>A array of string tags for the health check.</returns>
        static member Tags = [| "diagnostic"; "internal"; "basic" |]
        
        /// <summary>The health check's name.</summary>
        /// <returns>A array of string tags for the health check.</returns>
        static member Name = "uptime"
        
        /// <summary>The health check's failure status.</summary>
        /// <returns>HealthStatus.Unhealthy</returns>
        static member FailureStatus = HealthStatus.Unhealthy
            
       