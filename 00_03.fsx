// Create named actor from extracted behavior

#r "nuget: Akkling"

open System
open Akkling

type Message =
    | Hi
    | Greet of string

let system = System.create "my-system" <| Configuration.defaultConfig ()

let greeterBehavior (mailbox: Actor<Message>) = // we can define behavior of the actor in a separate function
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | Hi -> printfn "Hello from F#!"
            | Greet name -> printfn $"Hello {name}"

            return! loop ()
        }

    loop ()

let greetingActor = spawn system "greeter" <| props greeterBehavior // spawn named actor from behavior

greetingActor <! Hi
greetingActor <! Greet "Jane"
