namespace Peeps.Extensions

open Microsoft.Extensions.Logging
open Peeps.PeepsLogger
open Peeps.Sqlite

[<AutoOpen>]
module LoggingBuilder =

    type ILoggingBuilder with

        member builder.AddPeeps(ctx : PeepsContext) =
            [ LoggerConfig.InfoConfig(ctx.DbWriter, 0)
              LoggerConfig.DebugConfig(ctx.DbWriter, 0)
              LoggerConfig.TraceConfig(ctx.DbWriter, 0)
              LoggerConfig.WarningConfig(ctx.DbWriter, 0)
              LoggerConfig.ErrorConfig(ctx.DbWriter, 0) ]
            |> List.fold (fun (b: ILoggingBuilder) config -> b.AddProvider(new LoggerProvider(config))) builder
