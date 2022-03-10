namespace Peeps.Monitoring

open System
open System.IO
open Freql.Sqlite
open Peeps.Monitoring

type RequestPost =
    { CorrelationReference: Guid
      IpAddress: string
      RequestTime: DateTime
      RequestSize: int64
      Url: string }

type ResponsePost =
    { CorrelationReference: Guid
      Size: int64
      ResponseCode: int
      Time: int64 }
    
type AppMetrics = {
    Requests: int64
    Errors: int64
    Criticals: int64
    BytesReceived: int64
    BytesSent: int64
}

module Internal =

    let requestsTableSql =
        """
    CREATE TABLE requests (
		correlation_reference TEXT NOT NULL,
        ip_address TEXT NOT NULL,
        request_time TEXT NOT NULL,
		request_size TEXT NOT NULL,
		url TEXT,
		response_size TEXT,
        response_code INTEGER,
        execution_time INTEGER,
		CONSTRAINT requests_PK PRIMARY KEY (correlation_reference)
	);
    """

    let saveRequest (qh: SqliteContext) (request: RequestPost) = qh.Insert("requests", request)

    let saveResponse (qh: SqliteContext) (response: ResponsePost) =
        let sql =
            """
        UPDATE requests
        SET response_size = @0, response_code = @1, execution_time = @2
        WHERE correlation_reference = @3
        """

        qh.ExecuteVerbatimNonQueryAnon(
            sql,
            [ response.Size
              response.ResponseCode
              response.Time
              response.CorrelationReference ]
        )
        |> ignore

[<RequireQualifiedAccess>]
type AgentMessage =
    | Request of RequestPost
    | Response of ResponsePost
    | Error of ResponsePost
    | Critical of ResponsePost * exn
    | GetMetrics of AsyncReplyChannel<AppMetrics>

type AgentState =
    { Writer: SqliteContext
      Requests: int64
      Errors: int64
      Criticals: int64
      BytesReceived: int64
      BytesSent: int64 }
    static member Create(qh: SqliteContext) =
        { Writer = qh
          Requests = 0L
          Errors = 0L
          Criticals = 0L
          BytesReceived = 0L
          BytesSent = 0L }

    member s.ToAppMetrics() = ({
        Requests = s.Requests
        Errors = s.Errors
        Criticals = s.Criticals
        BytesReceived = s.BytesReceived
        BytesSent = s.BytesSent
    }: AppMetrics)

type PeepsMonitorAgent(path: string, criticalHandlers: (ResponsePost -> exn -> unit) list) =

    let agent =
        MailboxProcessor<AgentMessage>.Start
            (fun inbox ->
                let rec loop (state) =
                    async {
                        let! msg = inbox.Receive()

                        let newState =
                            match msg with
                            | AgentMessage.Request r ->
                                //printfn $"*** Correlation request {r.CorrelationReference}. Route: {r.Url}. Size: {r.RequestSize}"
                                Internal.saveRequest state.Writer r

                                { state with
                                      Requests = state.Requests + 1L
                                      BytesReceived = state.BytesReceived + r.RequestSize }
                            | AgentMessage.Response r ->
                                //printfn
                                //    $"*** Correlation request {r.CorrelationReference} completed. Code: {r.ResponseCode}. Response size: {r.Size}. Time (ms): {r.Time}."
                                Internal.saveResponse state.Writer r

                                { state with
                                      BytesSent = state.BytesSent + r.Size }
                            | AgentMessage.Error r ->
                                Internal.saveResponse state.Writer r

                                { state with
                                      Errors = state.Errors + 1L
                                      BytesSent = state.BytesSent + r.Size }
                            | AgentMessage.Critical (r, exn) ->
                                Internal.saveResponse state.Writer r
                                
                                criticalHandlers |> List.iter (fun h -> h r exn)
                                { state with
                                      Errors = state.Criticals + 1L
                                      BytesSent = state.BytesSent + r.Size }
                            | AgentMessage.GetMetrics rc ->
                                state.ToAppMetrics() |> rc.Reply
                                state

                        return! loop (newState)
                    }

                // Create the db.
                let qh =
                    SqliteContext.Create(Path.Combine(path, $"metrics_{DateTime.UtcNow:yyyyMMddHHmmss}.db"))

                qh.ExecuteSqlNonQuery Internal.requestsTableSql
                |> ignore

                loop (AgentState.Create(qh)))

    member _.SaveRequest(correlationRef: Guid, ipAddress: string, size: int64, url: string) =
        { CorrelationReference = correlationRef
          IpAddress = ipAddress
          RequestTime = DateTime.UtcNow
          RequestSize = size
          Url = url }
        |> AgentMessage.Request
        |> agent.Post

    member _.SaveResponse(correlationRef: Guid, size: int64, responseCode: int, time: int64) =
        { CorrelationReference = correlationRef
          Size = size
          ResponseCode = responseCode
          Time = time }
        |> AgentMessage.Response
        |> agent.Post

    member _.SaveError(correlationRef: Guid, size: int64, responseCode: int, time: int64) =
        { CorrelationReference = correlationRef
          Size = size
          ResponseCode = responseCode
          Time = time }
        |> AgentMessage.Error
        |> agent.Post

    member _.SaveCritical(correlationRef: Guid, size: int64, responseCode: int, time: int64, exn: Exception) =
        ({ CorrelationReference = correlationRef
           Size = size
           ResponseCode = responseCode
           Time = time }, exn)
        |> AgentMessage.Critical
        |> agent.Post

    member _.GetMetrics() =
        agent.PostAndReply<AppMetrics>(fun rc -> AgentMessage.GetMetrics rc)