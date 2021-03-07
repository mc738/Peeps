namespace Peeps

open System

[<Obsolete("Old api")>]
type LogItem =
    { from: string
      message: string
      time: DateTime
      ``type``: ItemType }

and [<Obsolete("Old api")>] ItemType =
    | Success
    | Error
    | Information
    | Warning
    | Debug

/// A basic logging class.
[<Obsolete("Old api")>]
type Logger() =

    let getCCT itemType =
        match itemType with
        | Success -> (ConsoleColor.Green, "OK")
        | Error -> (ConsoleColor.Red, "ERROR")
        | Information -> (ConsoleColor.White, "INFO")
        | Warning -> (ConsoleColor.Yellow, "WARN")
        | Debug -> (ConsoleColor.Magenta, "DEBUG")

    let handleItem item =
        let (color, title) = getCCT item.``type``
        Console.ForegroundColor <- color
        printf "%s\t" title
        Console.ResetColor()
        let time = item.time.ToString()
        printfn "[%s] %s: %s" time item.from item.message
        true

    let listener =
        MailboxProcessor<LogItem>
            .Start(fun inbox ->
                let rec loop () =
                    async {

                        let! item = inbox.Receive()

                        let cont = handleItem item

                        if cont then return! loop ()
                    }

                loop ())

    member this.Post item = listener.Post item
