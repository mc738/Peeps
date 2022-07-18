namespace Peeps

module Core =

    open System
    open Microsoft.Extensions.Logging

    /// <summary>A record represent Peeps general settings</summary>
    type PeepsSettings = { OutputDirectory: string }

    /// <summary>Various console colors. Mainly for use on Linux, Windows doesn't work well with escape codes</summary>
    type PeepsConsoleColor =
        | Black
        | BrightBlack
        | Red
        | BrightRed
        | Green
        | BrightGreen
        | Yellow
        | BrightYellow
        | Blue
        | BrightBlue
        | Magenta
        | BrightMagenta
        | Cyan
        | BrightCyan
        | White
        | BrightWhite
        | Custom
        | Reset

        /// <summary>Get the escape code to set the console foreground color. For use on Linux.</summary>
        /// <returns>A string representing the escape code.</returns>
        member color.Foreground =
            match color with
            | Black -> "\u001b[30m"
            | BrightBlack -> "\u001b[30;1m"
            | Red -> "\u001b[31m"
            | BrightRed -> "\u001b[31;1m"
            | Green -> "\u001b[32m"
            | BrightGreen -> "\u001b[32;1m"
            | Yellow -> "\u001b[33m"
            | BrightYellow -> "\u001b[33;1m"
            | Blue -> "\u001b[34m"
            | BrightBlue -> "\u001b[34;1m"
            | Magenta -> "\u001b[35m"
            | BrightMagenta -> "\u001b[35;1m"
            | Cyan -> "\u001b[36m"
            | BrightCyan -> "\u001b[36;1m"
            | White -> "\u001b[37m"
            | BrightWhite -> "\u001b[37;1m"
            | Custom -> failwith "Not implemented."
            | Reset -> "\u001b[0m"

        /// <summary>Get the escape code to set the console background color. For use on Linux.</summary>
        /// <returns>A string representing the escape code.</returns>
        member color.Background =
            match color with
            | Black -> "\u001b[40m"
            | BrightBlack -> "\u001b[40;1m"
            | Red -> "\u001b[41m"
            | BrightRed -> "\u001b[41;1m"
            | Green -> "\u001b[42m"
            | BrightGreen -> "\u001b[42;1m"
            | Yellow -> "\u001b[43m"
            | BrightYellow -> "\u001b[43;1m"
            | Blue -> "\u001b[44m"
            | BrightBlue -> "\u001b[44;1m"
            | Magenta -> "\u001b[45m"
            | BrightMagenta -> "\u001b[45;1m"
            | Cyan -> "\u001b[46m"
            | BrightCyan -> "\u001b[46;1m"
            | White -> "\u001b[47m"
            | BrightWhite -> "\u001b[47;1m"
            | Custom -> failwith "Not implemented."
            | Reset -> "\u001b[0m"

        
        /// <summary>Create a string to with a foreground color. For us on Linux.</summary>
        /// <param name="test">The text to be rendered.</param>
        /// <returns>A string with the text and escape codes.</returns>
        member color.ForegroundText(text: string) =
            sprintf "%s%s%s" color.Foreground text PeepsConsoleColor.Reset.Foreground

    /// <summary>Peep log item types, these represent log item levels.</summary>
    type LogItemType =
        | Information
        | Debug
        | Trace
        | Error
        | Warning
        | Critical

        /// <summary>Create a LogItemType from a LogLevel</summary>
        /// <param name="level">A Microsoft.Extensions.Logging.LogLevel.</param>
        /// <returns>A LogItemType matching the level.</returns>
        static member FromLogLevel(level: LogLevel) =
            match level with
            | LogLevel.Critical -> LogItemType.Critical
            | LogLevel.Debug -> LogItemType.Debug
            | LogLevel.Error -> LogItemType.Error
            | LogLevel.Information -> LogItemType.Information
            | LogLevel.None -> LogItemType.Debug
            | LogLevel.Trace -> LogItemType.Trace
            | LogLevel.Warning -> LogItemType.Warning
            | _ -> LogItemType.Warning

        /// <summary>Get the PeepsConsoleColor associated with a LogItemType.</summary>
        /// <returns>A PeepsConsoleColor.</returns>
        member itemType.ConsoleColor =
            match itemType with
            | Information -> PeepsConsoleColor.BrightWhite
            | Debug -> PeepsConsoleColor.Magenta
            | Trace -> PeepsConsoleColor.White
            | Error -> PeepsConsoleColor.Red
            | Warning -> PeepsConsoleColor.Yellow
            | Critical -> PeepsConsoleColor.BrightRed
            
        /// <summary>Serialize the item type to a string.</summary>
        /// <returns>A string representing the item type.</returns>
        member itemType.Serialize() =
            match itemType with
            | LogItemType.Critical -> "critical"
            | LogItemType.Debug -> "debug"
            | LogItemType.Error -> "error"
            | LogItemType.Information -> "information"
            | LogItemType.Trace -> "trace"
            | LogItemType.Warning -> "warning"

    /// <summary>A log item used in Peeps.</summary>
    and PeepsLogItem =
        { TimeUtc: DateTime
          From: string
          Message: string
          ItemType: LogItemType }

        /// <summary>Create a new log item.</summary>
        /// <param name="level">A Microsoft.Extensions.Logging.LogLevel.</param>
        /// <param name="from">The name of item producer.</param>
        /// <param name="message">The message to be logged.</param>
        /// <returns>A new PeepsLogItem.</returns>
        static member Create(level: LogLevel, from: string, message: string) =
            { TimeUtc = DateTime.UtcNow
              From = from
              Message = message
              ItemType = LogItemType.FromLogLevel level }

        /// <summary>Render a log item for display on Unix systems.</summary>
        /// <returns>The rendered item message.</returns>
        member item.Rendered =
            item.ItemType.ConsoleColor.ForegroundText item.Message
            
        /// <summary>Get the item's timestamp.</summary>
        /// <returns>The item's timestamp as a int64.</returns>
        member pli.Timestamp = DateTimeOffset(pli.TimeUtc).ToUnixTimeMilliseconds()