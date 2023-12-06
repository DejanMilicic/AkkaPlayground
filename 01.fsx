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
