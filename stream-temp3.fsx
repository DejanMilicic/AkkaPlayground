#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams

let system = System.create "streams-sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()


let actor targetRef (m: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            targetRef <! msg + "!!!"
            return! loop ()
        }

    loop ()

let spawnActor targetRef =
    spawnAnonymous system <| props (actor targetRef)

let s =
    Source.actorRef OverflowStrategy.DropNew 1000
    |> Source.mapMaterializedValue (spawnActor)
    |> Source.toMat (Sink.forEach (fun s -> printfn "Received: %s" s)) Keep.left
    |> Graph.run mat

s <! "Boo"
