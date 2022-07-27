namespace Peeps.Monitoring

module Metrics =

    open System
    open Peeps.Monitoring.DataStores    
    
    type AppMetrics =
        { Requests: int64
          Errors: int64
          Criticals: int64
          BytesReceived: int64
          BytesSent: int64 }

    [<RequireQualifiedAccess>]
    type AgentMessage =
        | Request of RequestPost
        | Response of ResponsePost
        | Error of ResponsePost
        | Critical of ResponsePost * exn
        | GetMetrics of AsyncReplyChannel<AppMetrics>

    type AgentState =
        { //Writer: SqliteContext
          Requests: int64
          Errors: int64
          Criticals: int64
          BytesReceived: int64
          BytesSent: int64 }
        static member Create() =
            { //Writer = qh
              Requests = 0L
              Errors = 0L
              Criticals = 0L
              BytesReceived = 0L
              BytesSent = 0L }

        member s.ToAppMetrics() =
            ({ Requests = s.Requests
               Errors = s.Errors
               Criticals = s.Criticals
               BytesReceived = s.BytesReceived
               BytesSent = s.BytesSent }: AppMetrics)

    type PeepsMonitorAgent(cfg: MonitoringStoreConfiguration) =

        let agent =
            MailboxProcessor<AgentMessage>.Start
                (fun inbox ->
                    let rec loop (state) =
                        async {
                            let! msg = inbox.Receive()

                            let newState =
                                match msg with
                                | AgentMessage.Request r ->
                                    cfg.SaveRequest r

                                    ({ state with
                                        Requests = state.Requests + 1L
                                        BytesReceived = state.BytesReceived + r.RequestSize }: AgentState)
                                | AgentMessage.Response r ->
                                    cfg.SaveResponse r

                                    { state with BytesSent = state.BytesSent + r.Size }
                                | AgentMessage.Error r ->
                                    cfg.SaveResponse r

                                    { state with
                                        Errors = state.Errors + 1L
                                        BytesSent = state.BytesSent + r.Size }
                                | AgentMessage.Critical (r, exn) ->
                                    cfg.CriticalHandlers
                                    |> List.iter (fun h -> h r exn)

                                    { state with
                                        Errors = state.Criticals + 1L
                                        BytesSent = state.BytesSent + r.Size }
                                | AgentMessage.GetMetrics rc ->
                                    state.ToAppMetrics() |> rc.Reply
                                    state

                            return! loop (newState)
                        }
                        
                    cfg.MetricsInitialization ()
                    loop (AgentState.Create()))

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
               Time = time },
             exn)
            |> AgentMessage.Critical
            |> agent.Post

        member _.GetMetrics() =
            agent.PostAndReply<AppMetrics>(fun rc -> AgentMessage.GetMetrics rc)
