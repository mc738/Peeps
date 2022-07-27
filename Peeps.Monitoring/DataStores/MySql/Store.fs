namespace Peeps.Monitoring.DataStores.MySql

open System
open Freql.MySql
open Peeps.Monitoring.DataStores

module Store =

    open Peeps.Monitoring.DataStores.MySql.Persistence

    let saveRequest (ctx: MySqlContext) (request: RequestPost) =
        ({ CorrelationId = request.CorrelationReference.ToString("n")
           IpAddress = request.IpAddress
           RequestTimestamp = request.RequestTime
           RequestSize = request.RequestSize
           Url = request.Url
           ResponseSize = None
           ResponseCode = None
           ExecutionTime = None }: Parameters.NewRequest)
        |> Operations.insertRequest ctx
        |> ignore
        ctx.Close()

    let saveResponse (ctx: MySqlContext) (response: ResponsePost) =
        ctx.ExecuteAnonNonQuery(
            "UPDATE requests SET response_size = @0, response_code = @1, execution_time = @2 WHERE correlation_id = @3",
            [ response.Size
              response.ResponseCode
              response.Time
              response.CorrelationReference.ToString("n") ]
        )
        |> ignore
        ctx.Close()


    let saveCritical (ctx: MySqlContext) (response: ResponsePost) (exn: Exception) =
        saveResponse ctx response

        ({ CorrelationId = response.CorrelationReference.ToString("n")
           Message = exn.Message }: Parameters.NewCritical)
        |> Operations.insertCritical ctx
        |> ignore
        ctx.Close()


    let config (connectionString: string) =
        { MetricsInitialization = fun _ -> ()
          SaveRequest = saveRequest (MySqlContext.Connect connectionString)
          SaveResponse = saveResponse (MySqlContext.Connect connectionString)
          CriticalHandlers = [ saveCritical (MySqlContext.Connect connectionString) ] }

    type MySqlLogStore(ctx: MySqlContext) =

        member _.Log() = ()
