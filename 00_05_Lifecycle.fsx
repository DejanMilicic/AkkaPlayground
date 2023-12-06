// Actor lifecycle events

#r "nuget: Akkling"
#r "nuget: Akka.Serialization.Hyperion"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig ()

let actor (m: Actor<_>) =
    let rec loop () =
        actor {
            let! (msg: obj) = m.Receive()

            match msg with
            | LifecycleEvent e ->
                match e with
                | PreStart -> printfn $"\n\t\t Actor {m.Self} is starting... \n"
                | PostStop -> printfn $"\n\t\t Actor {m.Self} has stopped \n"
                | _ -> return Unhandled
            | x -> printfn $"\n\t\t {x} \n"

            return! loop ()
        }

    loop ()

let bref = spawn system "second-actor" <| props actor

bref <! "ok"
(retype bref) <! PoisonPill.Instance
