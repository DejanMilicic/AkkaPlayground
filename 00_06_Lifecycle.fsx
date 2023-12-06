// Actor lifecycle basics

#r "nuget: Akkling"
#r "nuget: Akka.Serialization.Hyperion"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

let actor (m: Actor<_>) =
    let rec loop () =
        actor {
            let! (msg: obj) = m.Receive()

            match msg with
            | :? LifecycleEvent as e ->
                match e with
                | PreStart -> printfn $"\n\t\t Actor {m.Self} is starting... \n"
                | PostStop -> printfn $"\n\t\t Actor {m.Self} has stopped \n"
                | _ -> return Unhandled
            | x -> printfn $"\n\t\t {x} \n"

            return! loop ()
        }

    loop ()

let aref = spawnAnonymous system <| props actor

aref <! "ok"
(retype aref) <! PoisonPill.Instance
