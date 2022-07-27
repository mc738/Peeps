namespace Peeps.Monitoring

open System
open System.IO
open System.IO.Pipes
open System.Security.Principal
open System.Text
open System.Threading

/// <summary>App streams are a way to stream diagnostic information out of applications.</summary>
module AppStreams =

    /// <summary>Magic bytes used at the start of an app steam message header.<summary>
    /// <returns>An array containing the bytes 14 and 6.</returns>
    let magicBytes = [| 14uy; 6uy |]

    /// <summary>A app stream message header.</summary>
    type MessageHeader =
        { UserDefinedField1: byte
          UserDefinedField2: byte
          Length: int }

        /// <summary>The header length.</summary>
        /// <returns>8</summary>
        static member HeaderLength = 8

        /// <summary>Create an app stream message header.</summary>
        /// <param name="length">The length of the message.</param>
        /// <returns>A new MessageHeader.</returns>
        static member Create(length: int) =
            {
                UserDefinedField1 = 1uy
                UserDefinedField2 = 2uy
                Length = length
            }
        
        /// <summary>Try and parse a message header from a byte array.</summary>
        /// <param name="buffer">The byte array to try and parse the header from.</param>
        /// <returns>A result containing the MessageHeader if successfully parsed ot an error message if not.</returns>
        static member TryParse(buffer: byte array) =
            match buffer.Length >= 8 with
            | true ->
                match buffer.[0] = 14uy && buffer.[1] = 6uy with
                | true ->
                    let len = BitConverter.ToInt32(buffer, 4)
                    printfn $"Len: {len}"

                    { UserDefinedField1 = buffer.[2]
                      UserDefinedField2 = buffer.[3]
                      Length = len }
                    |> Ok
                | false -> Error "Magic bytes do not match."
            | false -> Error $"Buffer is too short (length: {buffer.Length})"

        /// <summary>Serialize a message header to a byte array</summary>
        /// <returns>The serialized header as a byte array.</returns>
        member mh.Serialize() =
            Array.concat [ magicBytes
                           [| mh.UserDefinedField1
                              mh.UserDefinedField2 |]
                           BitConverter.GetBytes mh.Length ]

    module IO =
        
        let read size (stream: Stream) =
            let buffer = Array.zeroCreate size
            stream.Read(buffer, 0, size) |> ignore
            buffer

        let readString size (stream: Stream) =
            read size stream |> Encoding.UTF8.GetString

        let write (data: byte array) (stream: Stream) =
            stream.Write(data, 0, data.Length)
            stream.Flush()

        let writeString (value: string) (stream: Stream) =
            stream.Write(value |> Encoding.UTF8.GetBytes, 0, value.Length)
            stream.Flush()
            
        let readMessageHeader (stream: Stream) =
            let buffer = Array.zeroCreate 8
            stream.Read(buffer, 0, 8) |> ignore
            MessageHeader.TryParse buffer
            
        let waitForMessage (id: string) (stream: Stream) =
            printfn $"[{id}] Waiting for message."
            let buffer = Array.zeroCreate 2
            stream.Read(buffer, 0, 2) |> ignore
            match buffer = magicBytes with
            | true ->
                printfn "Message received."
                match readMessageHeader stream with
                | Ok mh ->
                    printfn $"[{id}] Message header received: {mh}"
                    let msg = readString mh.Length stream
                    printfn $"[{id}] Message: {msg}"
                    Ok msg
                | Error e ->
                    printfn $"[{id}] ERROR: {e}"
                    Error e
            | false -> Error "Magic bytes do not match"

    module Reader =
        
        let start (id:string) (stream: Stream) (handler: string -> unit) =
            
            let rec loop() =
                match IO.waitForMessage id stream with
                | Ok msg ->
                    handler msg
                    loop()
                | Error e ->
                    printfn $"{e}"
                    
            loop()

    module Writer =
        
        let start (id:string) (stream: Stream) = MailboxProcessor<string>.Start(fun inbox ->
            let rec loop() = async {
                let! msg = inbox.Receive()
                let body = msg |> Encoding.UTF8.GetBytes
                let header = MessageHeader.Create(body.Length)
                IO.write
                    (Array.concat [ header.Serialize(); body ])
                    stream
                return! loop()
            }
            loop())
        
    /// Currently the server only reads data.
    module Server =
        let serverThread (name: string) (direction: PipeDirection) (maxThreads: int) handler (data: obj) =
            use pipeServer =
                new NamedPipeServerStream(name, direction, maxThreads)

            let id = Thread.CurrentThread.ManagedThreadId

            printfn $"[{id}] Waiting for connection..."
            pipeServer.WaitForConnection()
            printfn $"[{id}] Connection."

            try
                Reader.start (id.ToString()) pipeServer handler
            with
            | ex -> printfn $"[{id}] ERROR: {ex.Message}"

            pipeServer.Close()

        let start name maxThreads handler =
            let servers =
                [ 0 .. (maxThreads - 1) ]
                |> List.map
                    (fun i ->
                        printfn $"Starting thread {i}"

                        let thread =
                            Thread(ParameterizedThreadStart(serverThread name PipeDirection.InOut maxThreads handler))

                        thread.Start()
                        thread)
                    
            let rec loop (servers: Thread list) =
                match servers.IsEmpty with
                | true -> ()
                | false ->
                    servers
                    |> List.map
                        (fun s ->
                            match s.Join(250) with
                            | true ->
                                printfn $"Server thread [{s.ManagedThreadId}] has finished. Closing thread."
                                None
                            | false -> Some s)
                    |> List.choose id
                    |> fun s -> loop (s)
            loop (servers)
            
    /// Currently the client only writes data.
    module Client =
        
        let connect name =
            let pipeClient =
                new NamedPipeClientStream(
                    ".",
                    name,
                    PipeDirection.InOut,
                    PipeOptions.None,
                    TokenImpersonationLevel.Impersonation
                )

            Console.WriteLine("Connecting to server...\n")
            pipeClient.Connect()
            Writer.start (id.ToString()) pipeClient