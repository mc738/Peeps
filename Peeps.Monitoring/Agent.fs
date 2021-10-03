namespace Peeps.Monitoring

open System
open System.IO
open Freql.Sqlite

type RequestPost =
    { CorrelationReference: Guid
      RequestTime: DateTime
      RequestSize: int64
      Url: string }

type ResponsePost =
    { CorrelationReference: Guid
      Size: int64
      ResponseCode: int
      Time: int64 }

module Internal =

    let requestsTableSql =
        """
    CREATE TABLE requests (
		correlation_reference TEXT NOT NULL,
        request_time TEXT NOT NULL,
		request_size TEXT NOT NULL,
		url TEXT,
		response_size TEXT,
        response_code INTEGER,
        execution_time INTEGER,
		CONSTRAINT requests_PK PRIMARY KEY (correlation_reference)
	);
    """

    let saveRequest (qh: QueryHandler) (request: RequestPost) = qh.Insert("requests", request)

    let saveResponse (qh: QueryHandler) (response: ResponsePost) =
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

type AgentMessage =
    | Request of RequestPost
    | Response of ResponsePost
    | GetRequestCount of AsyncReplyChannel<int64>


type AgentState = {
       Writer: QueryHandler
       Requests: int64
}

type PeepsMonitorAgent(path: string) =
    
    let agent =
        MailboxProcessor<AgentMessage>.Start
            (fun inbox ->
                let rec loop (state) =
                    async {
                        let! msg = inbox.Receive()

                        let newState =
                            match msg with
                            | AgentMessage.Request r ->
                                printfn $"*** Correlation request {r.CorrelationReference}. Route: {r.Url}. Size: {r.RequestSize}"
                                Internal.saveRequest state.Writer r
                                { state with Requests = state.Requests + 1L }
                            | AgentMessage.Response r ->
                                printfn
                                    $"*** Correlation request {r.CorrelationReference} completed. Code: {r.ResponseCode}. Response size: {r.Size}. Time (ms): {r.Time}."
                                Internal.saveResponse state.Writer r
                                state
                            | AgentMessage.GetRequestCount rc ->
                                rc.Reply state.Requests
                                state

                        return! loop (newState)
                    }

                // Create the db.
                
                let qh = QueryHandler.Create(Path.Combine(path, "metrics.db"))    
                qh.ExecuteSqlNonQuery Internal.requestsTableSql |> ignore
                loop ({ Writer = qh; Requests = 0L }))

    member _.SaveRequest(correlationRef: Guid, size: int64, url: string) =
        { CorrelationReference = correlationRef
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
        
    member _.RequestCount() =
        agent.PostAndReply<int64>(fun rc -> AgentMessage.GetRequestCount rc)