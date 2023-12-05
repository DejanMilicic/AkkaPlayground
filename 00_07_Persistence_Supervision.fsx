// Actor supervision with persistence

#r "nuget: Akkling"
#r "nuget: Akkling.Persistence"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Persistence"
#r "nuget: Akkling"
#r "nuget: Akka.Streams"

open System
open Akkling
open Akkling.Persistence
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

type CounterChanged = { Delta: int }

type CounterCommand =
    | Inc of int
    | Dec of int
    | GetState

type CounterMessage =
    | Command of CounterCommand
    | Event of CounterChanged

let counterBehavior (mailbox: Actor<CounterMessage>) =
    let rec loop state =
        actor {
            printf $"\nCurrent counter state: {state}\n\n"

            let! msg = mailbox.Receive()

            match msg with
            | Event(changed) ->
                printf "Event applied!\n"
                return! loop (state + changed.Delta)
            | Command(cmd) ->
                match cmd with
                | GetState ->
                    mailbox.Sender() <! state
                    return! loop state
                | Inc num when num > 0 -> return Persist(Event { Delta = num })
                | Inc num when num < 0 -> raise <| ArgumentOutOfRangeException()
                | Inc num when num = 0 -> raise <| ArgumentException()
                | Dec num -> return Persist(Event { Delta = -num })
        }

    loop 0


//
// let counter =
//     spawn system "counter-1"
//     <| propsPersist (fun mailbox ->


/////////////////////////


// let workerBehavior (mailbox: Actor<string>) =
//     let actorId = mailbox.Self.Path.Name

//     let rec loop commands =
//         actor {
//             let commandsHistory = "[" + String.concat "," commands + "]"
//             printfn $"\nActor {actorId} has commands history: {commandsHistory}\n"

//             let! message = mailbox.Receive()

//             match message with
//             | "null" -> raise <| ArgumentNullException()
//             | cmd when cmd.StartsWith "-" -> raise <| ArgumentOutOfRangeException()
//             | cmd when Char.IsPunctuation(cmd[0]) -> raise <| ArgumentException()
//             | _ -> ()

//             return! loop (message :: commands)
//         }

//     loop ([])

type Message =
    | CreateActor of string
    | ActorCommand of string * CounterMessage

let supervisingBehavior (mailbox: Actor<Message>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | CreateActor actorId -> spawn mailbox actorId <| propsPersist counterBehavior |> ignore
            | ActorCommand(actorId, cmd) ->
                let actor = select mailbox actorId
                actor <! cmd

            return! loop ()
        }

    loop ()

let strategy () =
    Strategy.OneForOne(
        (fun ex ->
            printfn "\nInvoking supervision strategy\n"

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

// starting supervising actor
// that can create new actors and send them commands
// also, monitors children, and can react upon their failures
let supervisor =
    spawn system "runner"
    <| { props supervisingBehavior with
           SupervisionStrategy = Some(strategy ()) }

// supervising strategy

// "null"   -> ArgumentNullException        -> stop actor       -> actor is dead, messages not delivered anymore
// "-"      -> ArgumentOutOfRangeException  -> restart actor    -> actor is restarted, state is lost
// ".xxx"   -> ArgumentException            -> resume actor     -> actor is resumed, state is preserved

supervisor <! CreateActor "counter"

supervisor <! ActorCommand("counter", Inc 1 |> Command)
supervisor <! ActorCommand("counter", Inc 0 |> Command)
supervisor <! ActorCommand("counter", Inc -1 |> Command)

// actor restarted, state is lost
supervisor <! ActorCommand("counter", "-5")

// actor resumed, state is preserved
supervisor <! ActorCommand("counter", ".2")

// actor stopped, messages not delivered anymore
supervisor <! ActorCommand("counter", "null")
