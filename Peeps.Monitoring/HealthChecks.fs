namespace Peeps.Monitoring

open System
open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Diagnostics.HealthChecks

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

    type PeepsMemoryHealthCheck(total) =

        interface IHealthCheck with

            member this.CheckHealthAsync(context, cancellationToken) =
                let allocated = GC.GetTotalMemory(false)
                let totalAllocated = GC.GetTotalAllocatedBytes(false)

                //Microsoft.AspNetCore.Hosting
                //
                //System.Diagnostics.Tracing.Even

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
                
        static member Tags = [| "diagnostic"; "memory"; "internal"; "full" |]
        
        static member Name = "memory"
        
        static member FailureStatus = HealthStatus.Unhealthy

    type PeepsUptimeHealthCheck(maxRunTime, startTime: DateTime) =

        interface IHealthCheck with
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

        static member Tags = [| "diagnostic"; "internal"; "basic" |]
        
        static member Name = "uptime"
        
        static member FailureStatus = HealthStatus.Unhealthy