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

let supervisingBehavior (mailbox: Actor<Message>) =
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
    <| { props supervisingBehavior with
           SupervisionStrategy = Some(strategy ()) }

// supervising strategy

// "null"   -> ArgumentNullException        -> stop actor       -> actor is dead, messages not delivered anymore
// "-"      -> ArgumentOutOfRangeException  -> restart actor    -> actor is restarted, state is lost
// ".xxx"   -> ArgumentException            -> resume actor     -> actor is resumed, state is preserved

supervisor <! CreateActor "actor1"

supervisor <! ActorCommand("actor1", "1") //    message is processed
supervisor <! ActorCommand("actor1", "2") //    message is processed
supervisor <! ActorCommand("actor1", "3") //    message is processed
supervisor <! ActorCommand("actor1", "-5") //   actor restarted, state is lost
supervisor <! ActorCommand("actor1", ".2") //   actor resumed, state is preserved
supervisor <! ActorCommand("actor1", "null") // actor stopped, messages not delivered anymore
