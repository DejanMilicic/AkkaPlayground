#r "nuget: Akkling"
#r "nuget: Akkling.Streams"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"
#r "nuget: FSharp.Control.AsyncSeq"

open System
open System.Net.WebSockets
open System.Threading
open System.Text
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams
open FSharp.Control
open Akka.Actor

let system = System.create "scheduler" <| Configuration.defaultConfig ()

let mat = system.Materializer()

//=========================================================================================================

type GreetingActorMsg = Message of string

type GreetingActor() as g =
    inherit ReceiveActor()

    do
        g.Receive<GreetingActorMsg>(fun (greet: GreetingActorMsg) ->
            match greet with
            | Message msg -> printfn $"Hello {msg}")

let greeter = system.ActorOf<GreetingActor> "greeter"

"World" |> Message |> greeter.Tell
greeter.Tell <| Message "World"
Message "World 2" |> greeter.Tell

system.Scheduler.ScheduleTellRepeatedly(
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5),
    greeter,
    Message "World 2",
    ActorRefs.NoSender
)


//=========================================================================================================

type GreetingActor2() =
    inherit ReceiveActor()

    do
        base.Receive<GreetingActorMsg> (function
            | Message msg -> printfn $"Hello {msg}")


let greeter2 = system.ActorOf<GreetingActor2> "greeter"

"World" |> Message |> greeter2.Tell

system.Scheduler.ScheduleTellRepeatedly(
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5),
    greeter2,
    Message "Alternative World",
    ActorRefs.NoSender
)
