// supervision

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

type Command3 = 
    | Command of string
    | LifecycleEvent of LifecycleEvent

type Command = 
    | CreateActor of string
    | ActorCommand of string * string
    | CreateLifecycleActor of string
    | LifecycleActorCommand of string * Command3

let workerActor (mailbox:Actor<string>) =
    let actorId = mailbox.Self.Path.Name
    let rec loop commands =
        actor {
            printfn "Actor %s has commands history: [%s]" actorId <| String.concat "|" commands
            let! message = mailbox.Receive ()

            match message with
            | "null" -> raise <| ArgumentNullException()
            | cmd when cmd.StartsWith "-" -> raise<| ArgumentOutOfRangeException()
            | cmd when Char.IsPunctuation(cmd[0]) -> raise <| ArgumentException()
            | _ -> ()

            return! loop (message :: commands)
        }

    loop ([])

// let workerActor2 (mailbox:Actor<_>) = // to be able to process life cycle evcents, we sacrifiece strong typing
//     let actorId = mailbox.Self.Path.Name
//     let rec loop commands =
//         actor {
//             printfn "Actor %s has commands history: [%s]" actorId <| String.concat "|" commands
//             let! (message : obj) = mailbox.Receive ()

//             match message with
//             | :? string as cmd -> 
//                 match cmd with
//                 | "null" -> raise <| ArgumentNullException()
//                 | cmd when cmd.StartsWith "-" -> raise <| ArgumentOutOfRangeException()
//                 | cmd when Char.IsPunctuation(cmd[0]) -> raise <| ArgumentException()
//                 | _ -> ()
//                 return! loop (cmd :: commands)

//             | LifecycleEvent e ->
//                 printfn "Lifecycle event %A" e
//                 match e with
//                 | PreRestart (_, message) when (message :? string) ->
//                     let failedCmd = message :?> string
//                     if failedCmd.StartsWith "-" then
//                         mailbox.Self <! box (failedCmd.Substring 1)
//                 | _ -> ()
//             | _ -> ()

//             return! loop commands
//         }

//     loop ([])



let workerActor3 (mailbox:Actor<Command3>) =
    let actorId = mailbox.Self.Path.Name
    let rec loop commands =
        actor {
            printfn "Actor %s has commands history: [%s]" actorId <| String.concat "|" commands
            let! message = mailbox.Receive ()

            match message with
            | Command cmd ->
                match cmd with
                | "null" -> raise <| ArgumentNullException()
                | cmd when cmd.StartsWith "-" -> raise <| ArgumentOutOfRangeException()
                | cmd when Char.IsPunctuation(cmd.[0]) -> raise <| ArgumentException()
                | _ -> ()

                return! loop (cmd :: commands)

            | LifecycleEvent e ->
                Console.WriteLine "AAAAAAAAAAAAAAA"
                printfn "Lifecycle event %A" e
                match e with
                | PreRestart (_, message) when (message :? string) ->
                    let failedCmd = message :?> string
                    printf $"YYY- {failedCmd}"
                    if failedCmd.StartsWith "-" then
                        printf $"XXX- {failedCmd.Substring 1}"
                        mailbox.Self <! (Command <| (failedCmd.Substring 1))
                | PreStart -> printfn "Actor %A has started" mailbox.Self
                | PostStop -> printfn "Actor %A has stopped" mailbox.Self
                | _ -> return Unhandled                

                return! loop commands
        }

    loop ([])

let supervisingActor (mailbox:Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive ()
            
            match message with
            | CreateActor actorId -> 
                spawn mailbox actorId <| props workerActor |> ignore
            | ActorCommand (actorId, cmd) ->
                let actor = select mailbox actorId
                actor <! cmd

            | CreateLifecycleActor actorId -> 
                spawn mailbox actorId <| props workerActor3 |> ignore
            | LifecycleActorCommand (actorId, cmd: Command3) ->
                let actor = select mailbox actorId
                actor <! cmd

            return! loop ()
        }

    loop()

let strategy () = 
    Strategy.OneForOne((fun ex ->
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
    | _ -> Directive.Escalate), 3, TimeSpan.FromSeconds(10.))

let supervisor = spawn system "runner" <| { props supervisingActor with SupervisionStrategy = Some (strategy ()) }

supervisor <! CreateActor "1"
supervisor <! CreateActor "2"

supervisor <! ActorCommand ("1", "1")
supervisor <! ActorCommand ("2", "1")
supervisor <! ActorCommand ("2", "-5")
supervisor <! ActorCommand ("2", "2")

/////

supervisor <! CreateLifecycleActor "L1"
supervisor <! CreateLifecycleActor "L2"

supervisor <! LifecycleActorCommand ("L1", Command "1")
supervisor <! LifecycleActorCommand ("L2", Command "1")
supervisor <! LifecycleActorCommand ("L2", Command "-5")
supervisor <! LifecycleActorCommand ("L2", Command ".5")
supervisor <! LifecycleActorCommand ("L2", Command "2")