namespace Peeps.Monitoring

open System

type RequestPost =
    { CorrelationReference: Guid
      Size: int64
      Url: string }

type ResponsePost =
    { CorrelationReference: Guid
      Size: int64
      ResponseCode: int
      Time: int64 }

type AgentMessage =
    | Request of RequestPost
    | Response of ResponsePost


type PeepsMonitorAgent(path: string) =

    let agent =
        MailboxProcessor<AgentMessage>.Start
            (fun inbox ->
                let rec loop () =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | AgentMessage.Request r ->
                            printfn $"*** Correlation request {r.CorrelationReference}. Route: {r.Url}. Size: {r.Size}"
                        | AgentMessage.Response r ->
                            printfn $"*** Correlation request {r.CorrelationReference} completed. Code: {r.ResponseCode}. Response size: {r.Size}. Time (ms): {r.Time}."
                        return! loop ()
                    }

                loop ())

    member _.SaveRequest(correlationRef: Guid, size: int64, url: string) =
        { CorrelationReference = correlationRef
          Size = size
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
