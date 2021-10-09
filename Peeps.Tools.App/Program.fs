// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Freql.Sqlite
open Peeps.Tools.InfrastructureMapping
open Peeps.Tools.InfrastructureMapping.Store

let test path =
    let fullPath =
        Path.Combine(path, $"infrastructure_map-{DateTime.UtcNow:yyyyMMddHHmmss}")

    let qh = QueryHandler.Create fullPath
    Store.initialize qh

    Store.addComponent
        qh
        ({ Id = "test_1"
           Name = "Test 1"
           Description = "Test component"
           X = 10
           Y = 10 }: Records.Component)
        
    let c = Store.getComponent qh "test_1"
        
    printfn $"{c}"


// Define a function to construct a message to print
let from whom = sprintf "from %s" whom

[<EntryPoint>]
let main argv =
    test "C:\\ProjectData\\Peeps"
    //let message = from "F#" // Call the function
    //printfn "Hello world %s" message
    0 // return an integer exit code
