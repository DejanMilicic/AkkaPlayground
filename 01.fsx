// https://github.com/Horusiath/Akkling/blob/master/examples/basic.fsx
// https://www.seventeencups.net/posts/building-a-mud-with-f-sharp-and-akka-net-part-one/
//  https://github.com/17cupsofcoffee/AkkaMUD
// https://github.com/akkadotnet/akka.net/blob/dev/src/examples/FSharp.Api/Greeter.fs

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

// first, create a system
let system = Configuration.defaultConfig() |> System.create "system"

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

// or, define explicit behavior loop of an actor
let greeterBehavior (mailbox: Actor<GreeterMsg>) =
    let rec loop () = actor {
        let! msg = mailbox.Receive ()

        match (msg : GreeterMsg) with
        | Hello name -> printf $"Hello, {name}\n"
        | Goodbye name -> printf $"Goodbye, {name}\n"

        return! loop()
    }
    loop ()

// and spawn actor using it
let greeterRef = spawnAnonymous system (props greeterBehavior)

// send messages to the actor
greeterRef <! Hello "Joe"
greeterRef <! Goodbye "Joe"

(*
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