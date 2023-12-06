// Effects

#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

// loop() is a recursive function that will be called for each message
// upon receiving a message, you can also
// - decide to "Ignore" it, which means you accepted it, but you will not process it
// - decide to "Stop", which will stop the actor
// - explicitly react "Unhandled", which will place message in a dead letter queue

let advancedActor =
    spawnAnonymous system
    <| props (fun mailbox ->
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg with
                | "stop" -> return Stop
                | "ignore" -> return Ignore
                | "unhandled" -> return Unhandled
                | x ->
                    printfn $"{x}"
                    return! loop ()
            }

        loop ())

advancedActor <! "content" //   message will be processed
advancedActor <! "ignore" //    message will be ignored
advancedActor <! "content" //   after ignoring previous message, actor will continue to process messages
advancedActor <! "unhandled" // message will be unhandled and will end up in a dead letter queue
advancedActor <! "stop" //      actor will stop processing messages
advancedActor <! "content" //   since actor is stopped, it will not process any messages, hence this one will end up in a dead letter queue
