// https://github.com/Horusiath/Akkling/blob/master/examples/basic.fsx
// https://www.seventeencups.net/posts/building-a-mud-with-f-sharp-and-akka-net-part-one/
//  https://github.com/17cupsofcoffee/AkkaMUD
// https://github.com/akkadotnet/akka.net/blob/dev/src/examples/FSharp.Api/Greeter.fs
// https://github.com/object/akkling-net-fest

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

// create a top level actor "System"
// https://getakka.net/articles/intro/getting-started/tutorial-1.html
let system = System.create "my-system" <| Configuration.defaultConfig ()

// definition of message that our actor can receive
type Message =
    | Greet of string
    | Hi

//=====================================================

// spinning up an actor can be shortened

// define message handler with same behavior
let handler msg = //                            Message -> Effect<'Message>
    match msg with
    | Greet name -> printfn $"Hello {name}"
    | Hi -> printfn "Hello from F#!"
    |> ignored //                               instead of unit, return Actor Effect

// actorOf is a helper that will wrap up message handler into an actor
// adding mailbox.Receive(), loop() and other stuff
let greetingActor2 = spawnAnonymous system <| props (actorOf handler)

greetingActor2 <! Hi
greetingActor2 <! Greet "Jane"

//=====================================================

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
