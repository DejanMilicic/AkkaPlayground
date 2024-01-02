#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akkling
open System
open Akka
open Akka.Actor

let system = System.create "scheduler" <| Configuration.defaultConfig ()

//=================================================

type TriggerActorMsg =
    | StartEmittingMessage
    | StopEmittingMessage
    | EmitMessage of string

let emittingActor (mailbox: Actor<TriggerActorMsg>) =
    let rec loop (cancelable: Option<ICancelable>) =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | StartEmittingMessage ->
                cancelable |> Option.iter (fun c -> c.Cancel())

                let newCancelable =
                    mailbox.UntypedContext.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                        TimeSpan.FromSeconds(1.0),
                        TimeSpan.FromSeconds(1.0),
                        mailbox.UntypedContext.Self,
                        EmitMessage "tick",
                        Nobody.Instance
                    )

                return! loop (Some newCancelable)
            | StopEmittingMessage ->
                cancelable |> Option.iter (fun c -> c.Cancel())
                return! loop None
            | EmitMessage msg ->
                printfn $"Emitting message {msg}"
                return! loop cancelable
        }

    loop None

let myActor = spawnAnonymous system <| props emittingActor


myActor <! StartEmittingMessage

myActor <! StopEmittingMessage
