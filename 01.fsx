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
let system = Configuration.defaultConfig () |> System.create "my-system"

// definition of message that our actor can receive
type Message =
    | Greet of string
    | Hi

//=====================================================

// basic actor that will be able to accept messages of type Message
// and react to them by printing out a greeting

// define actor and spawn it into the system
let greetingActor = //                                      greetingActor - this is reference to an actor
    spawnAnonymous system //                                spawnAnonymous - will create an actor that has no name
    <| props (fun mailbox -> //                             props - define behavior of the actor
        let rec loop () = //                                loop - define recursive loop of the actor
            actor { //                                      actor - ActorBuilder that will build an actor from message handler
                let! msg = mailbox.Receive() //             Receive - fetch next message from mailbox, if any

                match msg with //                           thanx to F# type inference, this will be stronly typed
                | Greet name -> printfn $"Hello {name}"
                | Hi -> printfn "Hello from F#!"

                return! loop () //                          return! - will return result of the actor, and continue with the loop
            }

        loop ()) //                                         call loop() to start the actor

// send messages to the actor
greetingActor <! Greet "Joe"
greetingActor <! Hi

//=====================================================

// spinning up an actor can be shortened

// define message handler with same behavior
let handler msg =
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

// or, define explicit behavior loop of an actor
let greeterBehavior (mailbox: Actor<Message>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | Greet name -> printfn $"Hello {name}"
            | Hi -> printfn "Hello from F#!"

            return! loop ()
        }

    loop ()

// and spawn actor using it
let greeterRef = spawnAnonymous system (props greeterBehavior)

// send messages to the actor
greeterRef <! Hello "Joe"
greeterRef <! Goodbye "Joe"

// creating named actor
// let greetingActor2 = spawn system "greetingActor2" <| props (actorOf handler)

(*

// stop
// unhandled
// loop or not
// above loop() you can initialize code - use it later on with stateful actors

let behavior (m:Actor<_>) =
    let rec loop () = actor {
        let! msg = m.Receive ()
        match msg with
        | "stop" -> return Stop
        | "unhandle" -> return Unhandled
        | x ->
            printfn "%s" x
            return! loop ()
    }
    loop ()
*)

(*
let greeter = spawn system "greeter" <| props(fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()

        match (msg : Message) with
        | Hello name -> printf $"Hello, {name}\n"
        | Goodbye name -> printf $"Goodbye, {name}\n"

        return! loop()
    }
    loop())    
*)
