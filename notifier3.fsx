#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akkling
open System
open Akka
open Akka.Actor
open Akka.Streams.Dsl
open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams

let system = System.create "scheduler" <| Configuration.defaultConfig ()

//=================================================

type TriggerActorMsg =
    | StartEmittingMessage of IActorRef
    | StopEmittingMessage
    | EmitMessage of DateOnly

let emittingActor (mailbox: Actor<TriggerActorMsg>) =
    let rec loop (emitTarget: IActorRef option, cancelable: Option<ICancelable>) =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | StartEmittingMessage target ->
                cancelable |> Option.iter (fun c -> c.Cancel())

                let newCancelable =
                    mailbox.UntypedContext.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                        TimeSpan.FromSeconds(1.0),
                        TimeSpan.FromSeconds(1.0),
                        mailbox.UntypedContext.Self,
                        EmitMessage(DateOnly.FromDateTime(DateTime.Now)),
                        ActorRefs.NoSender
                    )

                return! loop (Some target, Some newCancelable)
            | StopEmittingMessage ->
                cancelable |> Option.iter (fun c -> c.Cancel())
                return! loop (None, None)
            | EmitMessage date ->
                mailbox.UntypedContext.Sender.Tell date
                return! loop (emitTarget, cancelable)
        }

    loop (None, None)

let emittingActorRef = spawnAnonymous system <| props emittingActor

let bufferSize = 1000
let overflowStrategy = OverflowStrategy.DropNew
let source = Source.ActorRef<TriggerActorMsg>(bufferSize, overflowStrategy)



// Use ToMaterialized with Keep.Left to keep the materialized value of the source
let materializedRef = source.ToMaterialized(Sink.forEach (fun x -> printfn $"Received: {x}"), Keep.Left).Run(system)

// Send a message to the emitting actor to start the process
emittingActorRef <! StartEmittingMessage materializedRef




let flow: Flow<TriggerActorMsg, obj, NotUsed> =
    Flow.Create<TriggerActorMsg, NotUsed>()
    |> Flow.map (fun _ -> StartEmittingMessage)
    |> Flow.ask (TimeSpan.FromSeconds 3.0) emittingActorRef

let source = Source.singleton StartEmittingMessage

source
    .Via(flow)
    .To(Sink.forEach (fun x -> printfn $"Received: {x}"))
    .Run(system)

//=================================================

emittingActorRef <! StartEmittingMessage

emittingActorRef <! StopEmittingMessage
