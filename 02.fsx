#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

type Message =
    | Greet of string
    | Hi

let handler msg =
    match msg with
    | Greet name -> printfn $"Hello {name}"
    | Hi -> printfn "Hello from F#!"
    |> ignored

//=====================================================
// actors do not have to be anonymous, they can have names

let actorName = "greeter"

spawn system actorName <| props (actorOf handler) |> ignore

let actorRef =
    system
        .ActorSelection($"/user/{actorName}")
        .Ask<ActorIdentity>(new Identify(null), TimeSpan.FromSeconds(5.0))
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> _.Subject

actorRef.Tell(Greet "Yeti")
