// asking instead of telling

// besides telling actors what to do, by sending commands
// you can also ask actors for information, by sending queries

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling

let system = System.create "my-system" <| Configuration.defaultConfig ()

let greeter (mailbox: Actor<string>) = // we can define behavior of the actor in a separate function
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()
            let sender = mailbox.Sender()

            sender <! $"Hello, {msg}" // replying back to the sender

            return! loop ()
        }

    loop ()

let greeterActor = spawnAnonymous system <| props greeter // spawn actor from behavior

let askGreeter name : string =
    greeterActor <? name |> Async.RunSynchronously

askGreeter "John"
askGreeter "Paul"
