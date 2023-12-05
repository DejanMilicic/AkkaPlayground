// Actor supervision

#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

let workerBehavior (mailbox: Actor<string>) =
    let actorId = mailbox.Self.Path.Name

    let rec loop commands =
        actor {
            let commandsHistory = "[" + String.concat "," commands + "]"
            printfn $"Actor {actorId} has commands history: {commandsHistory}"

            let! message = mailbox.Receive()

            match message with
            | "null" -> raise <| ArgumentNullException()
            | cmd when cmd.StartsWith "-" -> raise <| ArgumentOutOfRangeException()
            | cmd when Char.IsPunctuation(cmd[0]) -> raise <| ArgumentException()
            | _ -> ()

            return! loop (message :: commands)
        }

    loop ([])

type Message =
    | CreateActor of string
    | ActorCommand of string * string

let supervisingActor (mailbox: Actor<Message>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | CreateActor actorId -> spawn mailbox actorId <| props workerBehavior |> ignore
            | ActorCommand(actorId, cmd) ->
                let actor = select mailbox actorId
                actor <! cmd

            return! loop ()
        }

    loop ()

let strategy () =
    Strategy.OneForOne(
        (fun ex ->
            printfn "Invoking supervision strategy"

            match ex with
            | :? ArgumentNullException ->
                printfn "Stopping actor"
                Directive.Stop
            | :? ArgumentOutOfRangeException ->
                printfn "Restarting actor"
                Directive.Restart
            | :? ArgumentException ->
                printfn "Resuming actor"
                Directive.Resume
            | _ -> Directive.Escalate),
        3,
        TimeSpan.FromSeconds(10.)
    )

let supervisor =
    spawn system "runner"
    <| { props supervisingActor with
           SupervisionStrategy = Some(strategy ()) }

supervisor <! CreateActor "actor1"

supervisor <! ActorCommand("actor1", "1")
supervisor <! ActorCommand("actor1", "2")
supervisor <! ActorCommand("actor1", "3")
supervisor <! ActorCommand("actor1", "-5")
supervisor <! ActorCommand("actor1", ".2")
supervisor <! ActorCommand("actor1", "null")
