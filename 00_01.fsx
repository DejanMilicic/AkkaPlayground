// Your first actor

// Let's create a simple actor that will greet us.
// it will accept string messages and print out a greeting

#r "nuget: Akkling"

open System
open Akkling

// create a top level actor "System"
let system = System.create "my-system" <| Configuration.defaultConfig ()

// spawn anonymous actor and get a reference to it
let greetingActor =
    spawnAnonymous system
    <| props (fun (mailbox: Actor<string>) -> //            props - define behavior of the actor
        let rec loop () =
            actor { //                                      actor - ActorBuilder that will build an actor from message handler
                let! msg = mailbox.Receive() //             Receive - fetch next message from mailbox, if any

                printfn $"Hello, {msg}" //                  process received message

                return! loop () //                          continue with the loop
            }

        loop () //                                          call loop() to start the actor
    )

// send message to the actor
greetingActor <! "Joe"
