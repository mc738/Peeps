namespace Peeps.Extensions

open Microsoft.Extensions.Logging
open Peeps.Logger
open Peeps.Sqlite

[<AutoOpen>]
module LoggingBuilder =

    type ILoggingBuilder with

        member builder.AddPeeps(ctx : PeepsContext) =
            [ LoggerConfig.InfoConfig(ctx.Actions, 0)
              LoggerConfig.DebugConfig(ctx.Actions, 0)
              LoggerConfig.TraceConfig(ctx.Actions, 0)
              LoggerConfig.WarningConfig(ctx.Actions, 0)
              LoggerConfig.ErrorConfig(ctx.Actions, 0) ]
            |> List.fold (fun (b: ILoggingBuilder) config -> b.AddProvider(new LoggerProvider(config))) builder
