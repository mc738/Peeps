namespace Peeps.Monitoring.DataStores.MySql

open Freql.MySql


module LogStore =
    
    open Peeps.Monitoring.DataStores.MySql.Persistence
    
    let action (connectionString: string) (item: Peeps.Core.PeepsLogItem) =
        let ctx = MySqlContext.Connect(connectionString)
        
        ({
           ItemType = item.ItemType.Serialize()
           ItemTimestamp = item.TimeUtc
           Name = item.From
           Message = item.Message
         }: Parameters.NewLogItem)
        |> Operations.insertLogItem ctx 
        |> ignore

