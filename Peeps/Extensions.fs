namespace Peeps.Extensions

open Microsoft.Extensions.Logging
open Peeps.PeepsLogger
open Peeps.Sqlite 

[<AutoOpen>]
module LoggingBuilder =

    type ILoggingBuilder with
        
        member builder.AddPeeps(dbWriter : DbWriter) =
            [
                LoggerConfig.InfoConfig(dbWriter, 0)
                LoggerConfig.DebugConfig(dbWriter, 0)
                LoggerConfig.TraceConfig(dbWriter, 0)
                LoggerConfig.WarningConfig(dbWriter, 0)
                LoggerConfig.ErrorConfig(dbWriter, 0)
                // TODO fix this, currently broken (don't know why).
                //LoggerConfig.CriticalConfig(dbWriter, 0)
            ] |> List.fold (fun (b : ILoggingBuilder) config -> b.AddProvider(new LoggerProvider(config))) builder
            
            