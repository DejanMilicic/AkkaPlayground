#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

// first, create a system
let system = System.create "system" <| Configuration.defaultConfig()

// messages that the greeter actor can handle
type GreeterMsg =
    | Hello of string
    | Goodbye of string

// define actor and spawn it into the system
let greeter = spawn system "greeter" <| props(fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()

        match (msg : GreeterMsg) with
        | Hello name -> printf $"Hello, {name}\n"
        | Goodbye name -> printf $"Goodbye, {name}\n"

        return! loop()
    }
    loop())

// send messages to the actor
greeter <! Hello "Joe"
greeter <! Goodbye "Joe"
