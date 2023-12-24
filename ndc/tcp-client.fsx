#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Reactive.Streams
open Akka.IO
open Akkling
open Akkling.Streams
open Akka.Streams.Dsl

let system = System.create "sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

let send = Source.singleton "Hello there" |> Source.map ByteString.FromString

let receive =
    Flow.empty<ByteString, _>
    |> Flow.mapMatValue ignore
    |> Flow.map string
    |> Flow.toSink (Sink.forEach (printfn "\n\tClient received: %s"))

let flow = Flow.ofSinkAndSource receive send |> Flow.mapMatValue ignore

async {
    let! connection =
        system.TcpStream()
        |> Tcp.outgoing "127.0.0.1" 5000
        |> Flow.joinMat flow Keep.left
        |> Graph.run mat

    printfn "\n\tClient connected to %A" connection.RemoteAddress
    System.Console.ReadLine() |> ignore
}
|> Async.RunSynchronously
