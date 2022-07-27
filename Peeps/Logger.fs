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

        /// <summary>Create a configuration for information log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member InfoConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Information, actions, eventId)

        /// <summary>Create a configuration for debug log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member DebugConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Debug, actions, eventId)
        
        /// <summary>Create a configuration for trace log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member TraceConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Trace, actions, eventId)

        /// <summary>Create a configuration for error log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member ErrorConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Error, actions, eventId)

        /// <summary>Create a configuration for warning log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member WarningConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Warning, actions, eventId)

        /// <summary>Create a configuration for critical log types.</summary>
        /// <param name="actions">A list of ListAction's to be run when an item is received.</param>
        /// <param name="">The event id.</param>
        /// <returns>A new LoggerConfig.</returns>
        static member CriticalConfig(actions, eventId) =
            LoggerConfig.Create(LogLevel.Critical, actions, eventId)

    /// <summary>A logger, implementing ILogger.</summary>
    type Logger(name: string, config: LoggerConfig) =

        interface ILogger with
        
            /// <summary>Begin an log scope</summary>
            /// <param name="state">A object representing the log state.</param>
            /// <returns>An IDisposable representing the scope.</returns>
            member this.BeginScope(state) = Unchecked.defaultof<IDisposable>
            
            /// <summary>Check if a log level is enabled.</summary>
            /// <param name="logLevel">The LogLevel to check.</param>
            /// <returns>A bool representing if the specific log level is enabled.</returns>
            member this.IsEnabled(logLevel) = logLevel = config.LogLevel

            /// <summary>Log an item.</summary>
            /// <param name="logLevel">The item's LogLevel.</param>
            /// <param name="eventId">The item's EventId.</param>
            /// <param name="state">The log state.</param>
            /// <param name="exception">An exception.</param>
            /// <param name="formatter">A formatter for the item.</param>
            /// <returns>Nothing.</returns>
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

    /// <summary>Logger provider for generating loggers.</summary>
    type LoggerProvider(config: LoggerConfig) =

        let mutable _loggers = ConcurrentDictionary<string, Logger>()

        interface ILoggerProvider with
        
            /// <summary>Create a logger for a category.</summary>
            /// <param name="categoryName">The category name.</param>
            /// <returns>A new ILogger.</returns>
            member this.CreateLogger(categoryName) =
                _loggers.GetOrAdd(categoryName, (fun name -> new Logger(name, config))) :> ILogger

            /// <summary>Dispose of the log provider</summary>
            member this.Dispose() = _loggers.Clear()

    /// <summary>The context in which Peeps will run.</summary>
    type PeepsContext =
        { Name: string
          OutputDirectory: string
          Actions: LoggerAction list }
        
        /// <summary>Create a new PeepsContext.</summary>
        /// <param name="outputDirectory">The base output directory.</param>
        /// <param name="name">The application's name.<param>
        /// <param name="actions">A list of LogAction's to be run when an item is received.</param>
        static member Create(outputDirectory, name, actions) =
            { Name = name
              OutputDirectory = outputDirectory
              Actions = actions }