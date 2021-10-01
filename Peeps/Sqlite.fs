module Peeps.Sqlite

open System
open System
open System.IO
open System.IO
open Microsoft.Data.Sqlite
open Peeps.Core

type SqliteContext =
    { CreatedOn: DateTime
      Path: string
      Name: string
      Reference: Guid }

    /// Create a new `SqliteContext` and associated database.
    static member Create(path, name) =
        let reference = Guid.NewGuid()
        let createdOn = DateTime.UtcNow

        let dbName =
            sprintf "%s_%s.log" name (reference.ToString())

        // TODO switch for FUlits
        let path = Path.Combine(path, dbName)

        printfn "Creating database at: %s" path
        // Create the database
        File.WriteAllBytes(path, Array.empty)
        printfn "Database initialized successful at: %s" (DateTime.Now.ToString("hh:mm dd/MM/yy"))

        { CreatedOn = createdOn
          Path = path
          Name = dbName
          Reference = reference }

    member ctx.CreateConnection() =
        let conn =
            new SqliteConnection(sprintf "Data Source=%s" ctx.Path)

        conn.Open()
        conn

type Query<'p, 'r> =
    { Sql: string
      ParameterMapper: Option<'p -> Map<string, obj>>
      ResultMapper: SqliteDataReader -> 'r }

    member query.Prepare(connection: SqliteConnection, parameters: Map<string, obj>) =
        use comm = new SqliteCommand(query.Sql, connection)

        parameters
        |> Map.map (fun k v -> comm.Parameters.AddWithValue(k, v))
        |> ignore

        comm.Prepare()
        comm

    member query.Execute(connection: SqliteConnection, parameters: 'p) =
        use comm =
            match query.ParameterMapper with
            | Some pm -> query.Prepare(connection, (pm parameters))
            | None -> new SqliteCommand(query.Sql, connection)
        //comm.Prepare() - TODO delete.
        use reader = comm.ExecuteReader()
        query.ResultMapper reader

[<RequireQualifiedAccess>]
module private Internal =
    let createLogsTableQuery =
        ({ Sql = """
           CREATE TABLE log_item (
	          item_type TEXT NOT NULL,
	          date_time_utc TEXT NOT NULL,
	          timestamp_utc INTEGER NOT NULL,
	          name TEXT NOT NULL,
	          message TEXT NOT NULL
           );
           """
           ParameterMapper = None
           ResultMapper = (fun _ -> ()) }: Query<unit, unit>)

    let addLogItemQuery =
        { Sql = """
          INSERT INTO log_item
          (item_type, date_time_utc, timestamp_utc, name, message)
          VALUES(@item_type, @date_time, @timestamp, @name, @message);
          """
          ParameterMapper =
              Some
                  ((fun (item: PeepsLogItem) ->
                      let itemType =
                          match item.ItemType with
                          | LogItemType.Critical -> "Critical"
                          | LogItemType.Debug -> "Debug"
                          | LogItemType.Error -> "Error"
                          | LogItemType.Information -> "Information"
                          | LogItemType.Trace -> "Trace"
                          | LogItemType.Warning -> "Warning"

                      Map.ofList [ "@item_type", itemType :> obj
                                   "@date_time", item.TimeUtc.ToString() :> obj
                                   "@timestamp",
                                   DateTimeOffset(item.TimeUtc)
                                       .ToUnixTimeMilliseconds() :> obj
                                   "@name", item.From :> obj
                                   "@message", item.Message :> obj ]))
          ResultMapper = (fun _ -> ()) }

    let initializeDb (conn: SqliteConnection) =
        [ createLogsTableQuery ]
        |> List.map (fun q -> q.Execute(conn, ()))

type DbWriter(path, name) =

    let mutable context = SqliteContext.Create(path, name)

    let getConnection =
        let conn = context.CreateConnection()
        conn.Open()
        conn

    let handleItem connection item =
        let _ =
            Internal.addLogItemQuery.Execute(connection, item)

        true

    let listener =
        printfn "Starting dbwriter."
        use conn = getConnection
        // Initialize database.
        Internal.initializeDb conn |> ignore

        MailboxProcessor<PeepsLogItem>
            .Start(fun inbox ->
                let rec loop () =
                    async {
                        // TODO add handling for renewing/new connections.
                        let! item = inbox.Receive()
                        use conn = getConnection
                        conn.Open() // NOTE - don't delete, needed.
                        let cont = handleItem conn item
                        if cont then return! loop ()
                    }

                printfn "Initializing Peeps database."
                // Get the connection and start listening.
                loop ())

    member writer.Write(item: PeepsLogItem) = listener.Post(item)

    member writer.Close() = printfn "Closing dbwriter."