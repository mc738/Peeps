namespace Peeps.Monitoring.DataStores.MySql.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.MySql

/// Module generated on 11/07/2022 19:07:56 (utc) via Freql.Sqlite.Tools.
[<RequireQualifiedAccess>]
module Records =
    /// A record representing a row in the table `criticals`.
    type Critical =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("correlationId")>] CorrelationId: string
          [<JsonPropertyName("message")>] Message: string }
    
        static member Blank() =
            { Id = 0
              CorrelationId = String.Empty
              Message = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `criticals` (
  `id` int NOT NULL AUTO_INCREMENT,
  `correlation_id` varchar(36) NOT NULL,
  `message` varchar(1000) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `criticals_FK` (`correlation_id`),
  CONSTRAINT `criticals_FK` FOREIGN KEY (`correlation_id`) REFERENCES `requests` (`correlation_id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              id,
              correlation_id,
              message
        FROM criticals
        """
    
        static member TableName() = "criticals"
    
    /// A record representing a row in the table `log_items`.
    type LogItem =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("itemType")>] ItemType: string
          [<JsonPropertyName("itemTimestamp")>] ItemTimestamp: DateTime
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("message")>] Message: string }
    
        static member Blank() =
            { Id = 0
              ItemType = String.Empty
              ItemTimestamp = DateTime.UtcNow
              Name = String.Empty
              Message = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `log_items` (
  `id` int NOT NULL AUTO_INCREMENT,
  `item_type` varchar(20) NOT NULL,
  `item_timestamp` datetime NOT NULL,
  `name` varchar(100) NOT NULL,
  `message` varchar(1000) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              id,
              item_type,
              item_timestamp,
              name,
              message
        FROM log_items
        """
    
        static member TableName() = "log_items"
    
    /// A record representing a row in the table `requests`.
    type Request =
        { [<JsonPropertyName("correlationId")>] CorrelationId: string
          [<JsonPropertyName("ipAddress")>] IpAddress: string
          [<JsonPropertyName("requestTimestamp")>] RequestTimestamp: DateTime
          [<JsonPropertyName("requestSize")>] RequestSize: int64
          [<JsonPropertyName("url")>] Url: string
          [<JsonPropertyName("responseSize")>] ResponseSize: int64 option
          [<JsonPropertyName("responseCode")>] ResponseCode: int option
          [<JsonPropertyName("executionTime")>] ExecutionTime: int option }
    
        static member Blank() =
            { CorrelationId = String.Empty
              IpAddress = String.Empty
              RequestTimestamp = DateTime.UtcNow
              RequestSize = 0L
              Url = String.Empty
              ResponseSize = None
              ResponseCode = None
              ExecutionTime = None }
    
        static member CreateTableSql() = """
        CREATE TABLE `requests` (
  `correlation_id` varchar(36) NOT NULL,
  `ip_address` varchar(20) NOT NULL,
  `request_timestamp` datetime NOT NULL,
  `request_size` bigint NOT NULL,
  `url` varchar(1000) NOT NULL,
  `response_size` bigint DEFAULT NULL,
  `response_code` int DEFAULT NULL,
  `execution_time` int DEFAULT NULL,
  PRIMARY KEY (`correlation_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              correlation_id,
              ip_address,
              request_timestamp,
              request_size,
              url,
              response_size,
              response_code,
              execution_time
        FROM requests
        """
    
        static member TableName() = "requests"
    

/// Module generated on 11/07/2022 19:07:56 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `criticals`.
    type NewCritical =
        { [<JsonPropertyName("correlationId")>] CorrelationId: string
          [<JsonPropertyName("message")>] Message: string }
    
        static member Blank() =
            { CorrelationId = String.Empty
              Message = String.Empty }
    
    
    /// A record representing a new row in the table `log_items`.
    type NewLogItem =
        { [<JsonPropertyName("itemType")>] ItemType: string
          [<JsonPropertyName("itemTimestamp")>] ItemTimestamp: DateTime
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("message")>] Message: string }
    
        static member Blank() =
            { ItemType = String.Empty
              ItemTimestamp = DateTime.UtcNow
              Name = String.Empty
              Message = String.Empty }
    
    
    /// A record representing a new row in the table `requests`.
    type NewRequest =
        { [<JsonPropertyName("correlationId")>] CorrelationId: string
          [<JsonPropertyName("ipAddress")>] IpAddress: string
          [<JsonPropertyName("requestTimestamp")>] RequestTimestamp: DateTime
          [<JsonPropertyName("requestSize")>] RequestSize: int64
          [<JsonPropertyName("url")>] Url: string
          [<JsonPropertyName("responseSize")>] ResponseSize: int64 option
          [<JsonPropertyName("responseCode")>] ResponseCode: int option
          [<JsonPropertyName("executionTime")>] ExecutionTime: int option }
    
        static member Blank() =
            { CorrelationId = String.Empty
              IpAddress = String.Empty
              RequestTimestamp = DateTime.UtcNow
              RequestSize = 0L
              Url = String.Empty
              ResponseSize = None
              ResponseCode = None
              ExecutionTime = None }
    
    
/// Module generated on 11/07/2022 19:07:56 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Operations =

    let buildSql (lines: string list) = lines |> String.concat Environment.NewLine

    /// Select a `Records.Critical` from the table `criticals`.
    /// Internally this calls `context.SelectSingleAnon<Records.Critical>` and uses Records.Critical.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectCriticalRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectCriticalRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Critical.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Critical>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Critical>` and uses Records.Critical.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectCriticalRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectCriticalRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Critical.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Critical>(sql, parameters)
    
    let insertCritical (context: MySqlContext) (parameters: Parameters.NewCritical) =
        context.Insert("criticals", parameters)
    
    /// Select a `Records.LogItem` from the table `log_items`.
    /// Internally this calls `context.SelectSingleAnon<Records.LogItem>` and uses Records.LogItem.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectLogItemRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectLogItemRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.LogItem.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.LogItem>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.LogItem>` and uses Records.LogItem.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectLogItemRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectLogItemRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.LogItem.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.LogItem>(sql, parameters)
    
    let insertLogItem (context: MySqlContext) (parameters: Parameters.NewLogItem) =
        context.Insert("log_items", parameters)
    
    /// Select a `Records.Request` from the table `requests`.
    /// Internally this calls `context.SelectSingleAnon<Records.Request>` and uses Records.Request.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectRequestRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectRequestRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Request.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Request>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Request>` and uses Records.Request.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectRequestRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectRequestRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Request.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Request>(sql, parameters)
    
    let insertRequest (context: MySqlContext) (parameters: Parameters.NewRequest) =
        context.Insert("requests", parameters)
    