namespace Peeps

module Logger =

    open System
    open System.Collections.Concurrent
    open Microsoft.Extensions.Logging
    open Peeps.Core

    type LoggerAction = PeepsLogItem -> unit

    /// <summary>The logger configuration.</summary>
    type LoggerConfig =
        { EventId: int
          LogLevel: LogLevel
          Actions: LoggerAction list }
        
        /// <summary>Create a new configuration.</summary>
        static member Create(logLevel, actions, eventId) =
            { EventId = eventId
              LogLevel = logLevel
              Actions = actions }

        static member InfoConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Information, actions, eventId)

        static member DebugConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Debug, actions, eventId)

        static member TraceConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Trace, actions, eventId)

        static member ErrorConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Error, actions, eventId)

        static member WarningConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Warning, actions, eventId)

        static member CriticalConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Critical, actions, eventId)

    type Logger(name: string, config: LoggerConfig) =

        interface ILogger with
            member this.BeginScope(state) = Unchecked.defaultof<IDisposable>
            member this.IsEnabled(logLevel) = logLevel = config.LogLevel

            member this.Log(logLevel, eventId, state, ``exception``, formatter) =
                match (this :> ILogger).IsEnabled(logLevel)
                      && (config.EventId = 0 || config.EventId = eventId.Id) with
                | true ->
                    // create the peeps log item
                    let message =
                        $"[%i{eventId.Id} %s{logLevel.ToString()}] %s{name} - %s{formatter.Invoke(state, ``exception``)}"

                    let item =
                        PeepsLogItem.Create(logLevel, name, message)
                    
                    config.Actions |> List.iter (fun a -> a item)
                | false -> () // Do nothing.

    type LoggerProvider(config: LoggerConfig) =

        let mutable _loggers = ConcurrentDictionary<string, Logger>()

        interface ILoggerProvider with
            member this.CreateLogger(categoryName) =
                _loggers.GetOrAdd(categoryName, (fun name -> new Logger(name, config))) :> ILogger

            member this.Dispose() = _loggers.Clear()

    type PeepsContext =
        { Name: string
          OutputDirectory: string
          Actions: LoggerAction list }
        static member Create(outputDirectory, name, actions) =
            { Name = name
              OutputDirectory = outputDirectory
              Actions = actions }