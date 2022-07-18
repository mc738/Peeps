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

        let addLogItem (ctx: SqliteContext) (item: PeepsLogItem) = ctx.Insert("log_items", LogItemRecord.FromLogItem item)

        let initialize (ctx: SqliteContext) (runId: Guid) (startedOn: DateTime) =
            [ runStatsTableSql
              logsTableSql ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

            ctx.Insert("run_stats", ({ RunId = runId.ToString(); StartedOn = startedOn }: RunStatsRecord))
            
        let create (path: string) (name: string) (runId: Guid) (startedOn: DateTime) =
            let logsPath = Path.Combine(path, "logs")
            
            if Directory.Exists logsPath |> not then Directory.CreateDirectory logsPath |> ignore
            let filename = $"{name}-{runId:N}.log"
            
            let ctx = SqliteContext.Create(Path.Combine(logsPath, filename))
            initialize ctx runId startedOn
            File.WriteAllText(Path.Combine(logsPath, ".peeps_lock"), filename)
            ctx
       
        type StoreAgentMessage =
            | LogItem of PeepsLogItem
            | ItemCount of AsyncReplyChannel<int64>
            | Ping of AsyncReplyChannel<unit>
            | Shutdown of AsyncReplyChannel<unit>
        
        let agent path name runId startedOn =
            
            printfn $"Starting `{name}` peeps logger store agent."
            printfn "Initializing Peeps database."
            let ctx = create path name runId startedOn
            
            MailboxProcessor<StoreAgentMessage>
                .Start(fun inbox ->
                    let rec loop(ctx: SqliteContext) =
                        async {
                            let! message = inbox.Receive()
                            match message with
                            | LogItem item ->
                                addLogItem ctx item
                                return! loop(ctx)
                            | ItemCount rc ->
                                ctx.ExecuteScalar("SELECT COUNT(*) FROM log_items") |> rc.Reply
                                return! loop(ctx)
                            | Ping rc ->
                                rc.Reply()
                                return! loop(ctx)
                            | Shutdown rc ->
                                // TODO handle shutdown
                                rc.Reply()
                        }
                    // Get the connection and start listening.
                    loop (ctx)) 
   
    /// <summary>An Sqlite-based log store.</summary>
    type LogStore(path, name, runId, startedOn) =
        let agent = Internal.agent path name runId startedOn
        
        /// <summary>Add a log item.</summary>
        /// <param name="item">A PeepsLogItem to be saved.</param>
        member ls.AddItem(item: PeepsLogItem) =
            agent.Post(Internal.StoreAgentMessage.LogItem item)
            
        /// <summary>Shut down the log store.</summary>
        /// <returns>Nothing.</summary>
        member ls.Shutdown() =
            agent.PostAndReply(Internal.StoreAgentMessage.Shutdown)
         
        /// <summary>Get the number of log items in the store.</summary>
        /// <returns>The number of items.</returns>
        member ls.ItemCount() =
            agent.PostAndReply(fun rc -> Internal.StoreAgentMessage.ItemCount rc)
            
        /// <summary>Get the time the log store was started on.<summary>
        /// <returns>A DateTime representing when the store was started.</returns>
        member ls.StartedOn = startedOn
        
        /// <summary>Get the run id of the log store.</summary>
        /// <returns>The log store's run id as a Guid.</returns>
        member ls.RunId = runId
        
        /// <summary>Get the path of the log store.</summary>
        /// <returns>The log store's path.</returns>
        member ls.Path = path
        
        /// <summary>Send a ping message to the agent and wait upto 5 seconds for a response.</summary>
        /// <returns>A unit if successful, if not an error message.</returns>
        member ls.CheckConnection() =
            match agent.TryPostAndReply(Internal.StoreAgentMessage.Ping, timeout = 5000) with
            | Some _ -> Ok ()
            | None -> Result.Error "Did not receive response in 5 seconds."