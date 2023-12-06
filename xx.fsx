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

let system = System.create "my-system" <| Configuration.defaultConfig ()

type Message =
    | Greet of string
    | Hi

let handler msg =
    match msg with
    | Greet name -> printfn $"Hello {name}"
    | Hi -> printfn "Hello from F#!"
    |> ignored // instead of unit, return Actor Effect

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
