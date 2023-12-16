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

let actor1 targetRef (m: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            printf "Actor1 Received: %s\n\n" msg
            targetRef <! msg + "!!!"
            return! loop ()
        }

    loop ()

let spawnActor1 targetRef =
    spawnAnonymous system <| props (actor1 targetRef)

let actor2 (m: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            printf "Actor2 Received: %s\n\n" msg
            return! loop ()
        }

    loop ()

let actor2ref = spawnAnonymous system <| props actor2


let s =
    Source.actorRef OverflowStrategy.DropNew 1000
    |> Source.mapMaterializedValue (spawnActor1)
    //|> Source.runForEach mat (fun s -> printfn $"Actor Returned: {s}\n\n")

    //|> Source.runForEach mat (fun s -> actor2ref <! s)
    |> Source.toMat (Sink.forEach (fun s -> actor2ref <! s)) Keep.left

    //|> Source.toMat Sink.ignore Keep.left
    |> Graph.run mat

s <! "Boo"


let s2: IActorRef<string> =
    Source.actorRef OverflowStrategy.DropNew 1000
    |> Source.mapMaterializedValue (spawnActor1)
    |> Source.toMat (Sink.forEach (fun s -> actor2ref <! s)) Keep.left
    |> Graph.run mat

s2 <! "Boo"
