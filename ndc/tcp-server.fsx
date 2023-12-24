#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akka.IO
open Akkling
open Akkling.Streams
open Akka.Streams.Dsl

let system = System.create "sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

//let handler = Flow.empty |> Flow.mapMatValue ignore

let handler =
    Flow.empty
    |> Flow.mapMatValue ignore
    |> Flow.via (Framing.delimiter true 256 (ByteString.FromString "\r\n"))
    |> Flow.map string
    |> Flow.iter (printfn "\n\tServer Received: %s")
    |> Flow.map (sprintf "%s, Server greets you!!!")
    |> Flow.map ByteString.FromString

// simple server

async {
    let! server =
        system.TcpStream()
        |> Tcp.bind "127.0.0.1" 5000
        |> Source.toMat
            (Sink.forEach (fun conn ->
                printfn "\n\tServer Accepted Stream from: %s" <| string conn.RemoteAddress
                conn.Flow |> Flow.join handler |> Graph.run mat |> ignore))
            Keep.left
        |> Graph.run mat

    printfn "Server listening on %A. Press Enter to stop ..." server.LocalAddress
    System.Console.ReadLine() |> ignore

    do! server.AsyncUnbind()
}
|> Async.RunSynchronously
