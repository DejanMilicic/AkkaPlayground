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

type Command =
    | Inc of int
    | Dec of int

type Message =
    | Command of Command
    | Event of CounterChanged

let counterBehavior (mailbox: Actor<Message>) =
    let rec loop state =
        actor {
            printf $"\nCurrent counter state: {state}\n"

            let! msg = mailbox.Receive()

            match msg with
            | Event changed ->
                printf "Event applied!\n"
                return! loop (state + changed.Delta)
            | Command cmd ->
                match cmd with
                | Inc num ->
                    if num > 0 then return Persist(Event { Delta = num })
                    else if num = 0 then raise <| ArgumentNullException()
                    else if num = -1 then raise <| ArgumentException()
                    else raise <| ArgumentOutOfRangeException()
                | Dec num ->
                    if num > 0 then return Persist(Event { Delta = -num })
                    else if num = 0 then raise <| ArgumentNullException()
                    else if num = -1 then raise <| ArgumentException()
                    else raise <| ArgumentOutOfRangeException()
        }

    loop 0

type SupervisorMessage =
    | CreateActor of string
    | ActorCommand of string * Message

let supervisor (mailbox: Actor<SupervisorMessage>) =
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
let supervisorActor =
    spawn system "runner"
    <| { props supervisor with
           SupervisionStrategy = Some(strategy ()) }

// supervising strategy

// 0    -> ArgumentNullException        -> stop actor       -> actor is dead, messages not delivered anymore
// -1   -> ArgumentException            -> resume actor     -> actor is resumed, state is preserved
// -2   -> ArgumentOutOfRangeException  -> restart actor    -> actor is restarted, state is reconstructed from journal

supervisorActor <! CreateActor "counter"

supervisorActor <! ActorCommand("counter", Inc 1 |> Command)
supervisorActor <! ActorCommand("counter", Inc 3 |> Command)

// actor resumed, state is preserved
supervisorActor <! ActorCommand("counter", Inc -1 |> Command)

// actor restarted, state is reconstructed
supervisorActor <! ActorCommand("counter", Inc -2 |> Command)

// actor stopped, messages not delivered anymore
supervisorActor <! ActorCommand("counter", Inc 0 |> Command)
