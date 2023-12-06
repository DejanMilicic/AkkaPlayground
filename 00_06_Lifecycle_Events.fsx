// Actor Lifecycle Events

#r "nuget: Akkling"
#r "nuget: Akka.Serialization.Hyperion"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

let worker (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! (message: obj) = mailbox.Receive()

            match message with
            | :? LifecycleEvent as e ->
                match e with
                | PreStart -> printfn $"\n\t\t Actor {mailbox.Self} is starting... \n"
                | PostStop -> printfn $"\n\t\t Actor {mailbox.Self} has stopped \n"
                | PreRestart(cause, msg) ->
                    printfn $"\n\t\t Actor RESTARTING due to '{cause.Message}' caused by message '{msg}' \n"
                | PostRestart(cause) -> printfn $"\n\t\t Actor RESTARTED due to '{cause.Message}' \n"
            | :? string as s when s = "fail" -> failwith "Simulated error"
            | _ ->
                printfn $"\n\t\t Actor {mailbox.Self} received message: '{message.ToString()}' \n"
                return! loop ()
        }

    loop ()

type SupervisorMessage =
    | CreateActor of string
    | ActorCommand of string * string

let supervisor (mailbox: Actor<SupervisorMessage>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | CreateActor actorId -> spawn mailbox actorId <| props worker |> ignore
            | ActorCommand(actorId, cmd) ->
                let actor = select mailbox actorId
                actor <! cmd

            return! loop ()
        }

    loop ()

let supervisionStrategy () =
    Strategy.OneForOne(fun _ ->
        printfn "\n\t\t Invoking supervision strategy\n"
        Directive.Restart)

let supervisorAct =
    spawn system "runner"
    <| { props supervisor with
           SupervisionStrategy = Some(supervisionStrategy ()) }

supervisorAct <! CreateActor "actor1"
supervisorAct <! ActorCommand("actor1", "1")
supervisorAct <! ActorCommand("actor1", "fail")
