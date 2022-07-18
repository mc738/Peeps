namespace Peeps

[<AutoOpen>]
module Extensions =

    open Microsoft.Extensions.Logging
    open Peeps.Logger

    [<AutoOpen>]
    module LoggingBuilder =

        type ILoggingBuilder with

            /// <summary>An extension to add Peeps to a ILoggingBuilder.</summary>
            /// <param name="ctx">The PeepsContext.</param>
            /// <returns>The updated ILoggingBuilder.</returns>
            member builder.AddPeeps(ctx : PeepsContext) =
                [ LoggerConfig.InfoConfig(ctx.Actions, 0)
                  LoggerConfig.DebugConfig(ctx.Actions, 0)
                  LoggerConfig.TraceConfig(ctx.Actions, 0)
                  LoggerConfig.WarningConfig(ctx.Actions, 0)
                  LoggerConfig.ErrorConfig(ctx.Actions, 0)
                  LoggerConfig.CriticalConfig(ctx.Actions, 0) ]
                |> List.fold (fun (b: ILoggingBuilder) config -> b.AddProvider(new LoggerProvider(config))) builder