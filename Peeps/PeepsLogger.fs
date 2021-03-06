namespace Peeps.PeepsLogger

open System
open System.Collections.Concurrent
open Microsoft.Extensions.Logging
open Peeps
open Peeps.Core
open Peeps.Sqlite

type LoggerConfig =
    { EventId: int
      LogLevel: LogLevel
      DbWriter: DbWriter
      LogToConsole: bool
      SaveLogs: bool }
    static member Create(logLevel, dbWriter, eventId) =
        { EventId = eventId
          LogLevel = logLevel
          DbWriter = dbWriter
          LogToConsole = true
          SaveLogs = true  }

    static member InfoConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Information, dbWriter, eventId)

    static member DebugConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Debug, dbWriter, eventId)
        
    static member TraceConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Trace, dbWriter, eventId)
        
    static member ErrorConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Error, dbWriter, eventId)
            
    static member WarningConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Warning, dbWriter, eventId)
       
    static member CriticalConfig(dbWriter, eventId) =
        LoggerConfig.Create(LogLevel.Critical, dbWriter, eventId)
        
        
type Logger(name: string, config: LoggerConfig) =

    interface ILogger with
        member this.BeginScope(state) = failwith "todo"
        member this.IsEnabled(logLevel) = logLevel = config.LogLevel

        member this.Log(logLevel, eventId, state, ``exception``, formatter) =
            match (this :> ILogger).IsEnabled(logLevel) && (config.EventId = 0 || config.EventId = eventId.Id) with
            | true ->
                // create the peeps log item
                let message = sprintf "[%i %s] %s - %s" eventId.Id (logLevel.ToString()) name (formatter.Invoke(state, ``exception``))
                
                let item = PeepsLogItem.Create(logLevel, name, message)
                
                printfn "%s" item.Rendered
                (config.DbWriter.Write(item))
                
            | false -> () // Do nothing.

type LoggerProvider(config: LoggerConfig) =
    
    let mutable _loggers = ConcurrentDictionary<string, Logger>()

    interface ILoggerProvider with
        member this.CreateLogger(categoryName) =
            _loggers.GetOrAdd(categoryName, (fun name -> new Logger(name, config))) :> ILogger

        member this.Dispose() = _loggers.Clear()