#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig ()

let actor (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | LifecycleEvent e ->
                match e with
                | PreStart -> printfn $"Actor {mailbox.Self} has started\n"
                | PostStop -> printfn $"Actor {mailbox.Self} has stopped\n"
                | _ -> return Unhandled
            | x -> printfn $"{x}"

            return! loop ()
        }

    loop ()

let aref: IActorRef<obj> = spawnAnonymous system <| props actor

aref <! "ok"
(retype aref) <! PoisonPill.Instance // sending PoisonPill to the actor will stop it
