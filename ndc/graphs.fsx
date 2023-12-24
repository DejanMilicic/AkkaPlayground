#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akka.IO
open Akkling
open Akkling.Streams
open Akkling.Streams.Operators
open Akka.Streams.Dsl
open Akka.Streams

let system = System.create "sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

Graph.create
<| fun b ->
    graph b {
        let! merge = MergePreferred<_> 1
        let! bcast = Broadcast 2
        let! source = Source.singleton 0
        let! flow = Flow.id |> Flow.map ((+) 1) |> Flow.iter (printfn "%d")
        let! sink = Sink.ignore

        b.From source =>> merge =>> flow =>> bcast =>> sink |> ignore
        b.From bcast =>> merge.Preferred |> ignore
    }
|> Graph.runnable
|> Graph.run mat

printfn "Graph consutructed ..."
System.Console.ReadLine() |> ignore
