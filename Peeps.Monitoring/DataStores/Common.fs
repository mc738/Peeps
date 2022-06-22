namespace Peeps.Monitoring.DataStores

open System
open Peeps.Core

[<AutoOpen>]
module Common =


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

    type Critical =
        { CorrelationReference: Guid
          Message: string }

    type MonitoringStoreConfiguration =
        { //LogStoreAction: PeepsLogItem -> unit
          MetricsInitialization: unit -> unit
          SaveRequest: RequestPost -> unit
          SaveResponse: ResponsePost -> unit
          CriticalHandlers: (ResponsePost -> exn -> unit) list }
