namespace Peeps.Monitoring.DataStores.Sqlite

open System
open Freql.Sqlite
open Peeps.Monitoring.DataStores

module Store =

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

        let criticalsTableSql =
            """
            CREATE TABLE criticals (
                correlation_reference TEXT NOT NULL,
                message TEXT NOT NULL,
                CONSTRAINT criticals_FK FOREIGN KEY (correlation_reference) REFERENCES requests(correlation_reference)
            )
            """

        let init (ctx: SqliteContext) _ =
            [ requestsTableSql; criticalsTableSql ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

        let saveRequest (ctx: SqliteContext) (request: RequestPost) = ctx.Insert("requests", request)

        let saveResponse (ctx: SqliteContext) (response: ResponsePost) =
            let sql =
                """
            UPDATE requests
            SET response_size = @0, response_code = @1, execution_time = @2
            WHERE correlation_reference = @3
            """

            ctx.ExecuteVerbatimNonQueryAnon(
                sql,
                [ response.Size
                  response.ResponseCode
                  response.Time
                  response.CorrelationReference ]
            )
            |> ignore

        let saveCritical (ctx: SqliteContext) (response: ResponsePost) (exn: Exception) =
            saveResponse ctx response

            ctx.Insert(
                "criticals",
                { CorrelationReference = response.CorrelationReference
                  Message = exn.Message }
            )

    let config (ctx: SqliteContext) =
        ({ //LogStoreAction = logStore
           MetricsInitialization = Internal.init ctx
           SaveRequest = Internal.saveRequest ctx
           SaveResponse = Internal.saveResponse ctx
           CriticalHandlers = [ Internal.saveCritical ctx ] }: MonitoringStoreConfiguration)
