namespace Peeps.LiveView

open Microsoft.AspNetCore.Builder
open Peeps.LiveView.Middleware

[<AutoOpen>]
module Extensions =

    type IApplicationBuilder with

        member builder.UsePeepsLiveView() =
            builder
                .UseWebSockets()
                .UseMiddleware<WebSocketMiddleware>()