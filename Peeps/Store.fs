namespace Peeps

open System
open System.IO
open Freql.Sqlite
open Peeps.Core

module Store =

    [<RequireQualifiedAccess>]
    module Internal =

        let runStatsTableSql =
            """
			CREATE TABLE run_stats (
	          run_id TEXT NOT NULL,
              started_on TEXT NOT NULL
           );
        """
        
        let logsTableSql =
            """
           CREATE TABLE log_items (
	          item_type TEXT NOT NULL,
	          date_time_utc TEXT NOT NULL,
	          timestamp_utc INTEGER NOT NULL,
	          name TEXT NOT NULL,
	          message TEXT NOT NULL
           );
           """


        type RunStatsRecord = {
            RunId: string
            StartedOn: DateTime
        }
        
        type LogItemRecord =
            { ItemType: string
              DateTimeUtc: DateTime
              TimestampUtc: int64
              Name: string
              Message: string }

            static member FromLogItem(item: PeepsLogItem) =
                { ItemType = item.ItemType.Serialize()
                  DateTimeUtc = item.TimeUtc
                  TimestampUtc = item.Timestamp
                  Name = item.From
                  Message = item.Message }

        let addLogItem (qh: QueryHandler) (item: PeepsLogItem) = qh.Insert("log_items", LogItemRecord.FromLogItem item)

        let initialize (qh: QueryHandler) (runId: Guid) (startedOn: DateTime) =
            [ runStatsTableSql
              logsTableSql ]
            |> List.map qh.ExecuteSqlNonQuery
            |> ignore

            qh.Insert("run_stats", ({ RunId = runId.ToString(); StartedOn = startedOn }: RunStatsRecord))
            
        let create (path: string) (name: string) (runId: Guid) (startedOn: DateTime) =
            let logsPath = Path.Combine(path, "logs")
            
            if Directory.Exists logsPath |> not then Directory.CreateDirectory logsPath |> ignore
            
            let qh = QueryHandler.Create(Path.Combine(path, "logs", $"{name}-{runId:N}.log"))
            initialize qh runId startedOn
            qh
       
        type StoreAgentMessage =
            | LogItem of PeepsLogItem
            | Shutdown of AsyncReplyChannel<unit>
        
        let agent path name runId startedOn =
            
            printfn $"Starting `{name}` peeps logger store agent."
            printfn "Initializing Peeps database."
            let qh = create path name runId startedOn
            
            MailboxProcessor<StoreAgentMessage>
                .Start(fun inbox ->
                    let rec loop(qh: QueryHandler) =
                        async {
                            let! message = inbox.Receive()
                            match message with
                            | LogItem item ->
                                addLogItem qh item
                                return! loop(qh: QueryHandler)
                            | Shutdown rc ->
                                // TODO handle shutdown
                                rc.Reply()
                        }
                    // Get the connection and start listening.
                    loop (qh)) 
    
    type LogStore(path, name, runId, startedOn) =
        let agent = Internal.agent path name runId startedOn
        
        member ls.AddItem(item: PeepsLogItem) =
            agent.Post(Internal.StoreAgentMessage.LogItem item)
            
        member ls.Shutdown() =
            agent.PostAndReply(Internal.Shutdown)
            
        member ls.StartedOn = startedOn
        
        member ls.RunId = runId