module Peeps.Core

open System
open Microsoft.Extensions.Logging

type PeepsSettings = { OutputDirectory: string }

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

    member color.ForegroundText(text: string) =
        sprintf "%s%s%s" color.Foreground text PeepsConsoleColor.Reset.Foreground

type LogItemType =
    | Information
    | Debug
    | Trace
    | Error
    | Warning
    | Critical

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

    member itemType.ConsoleColor =
        match itemType with
        | Information -> PeepsConsoleColor.BrightWhite
        | Debug -> PeepsConsoleColor.Magenta
        | Trace -> PeepsConsoleColor.White
        | Error -> PeepsConsoleColor.Red
        | Warning -> PeepsConsoleColor.Yellow
        | Critical -> PeepsConsoleColor.BrightRed

and PeepsLogItem =
    { TimeUtc: DateTime
      From: string
      Message: string
      ItemType: LogItemType }

    static member Create(level: LogLevel, from: string, message: string) =
        { TimeUtc = DateTime.UtcNow
          From = from
          Message = message
          ItemType = LogItemType.FromLogLevel level }

    member item.Rendered =
        item.ItemType.ConsoleColor.ForegroundText item.Message