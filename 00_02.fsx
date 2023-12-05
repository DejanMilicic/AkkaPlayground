// Strongly typed actor

// You can send strongly typed messages to the actor

#r "nuget: Akkling"

open System
open Akkling

type Message =
    | Hi
    | Greet of string

let system = System.create "my-system" <| Configuration.defaultConfig ()

let greetingActor = //
    spawnAnonymous system //
    <| props (fun (mailbox: Actor<Message>) ->
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg with
                | Hi -> printfn "Hello from F#!"
                | Greet name -> printfn $"Hello {name}"

                return! loop ()
            }

        loop ())

greetingActor <! Hi
greetingActor <! Greet "Jane"
