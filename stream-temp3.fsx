#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams
open Akka.Actor

let system = System.create "streams-sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

let actor1 targetRef (m: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            printf "\nActor1 Received: %s\n\n" msg
            targetRef <! msg + "!!!"
            return! loop ()
        }

    loop ()

let spawnActor1 targetRef =
    spawnAnonymous system <| props (actor1 targetRef)

let actor2 (m: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            printf "\nActor2 Received: %s\n\n" msg
            return! loop ()
        }

    loop ()

let actor2ref = spawnAnonymous system <| props actor2


let s =
    Source.actorRef OverflowStrategy.DropNew 1000
    |> Source.mapMaterializedValue (spawnActor1)
    //|> Source.runForEach mat (fun s -> printfn $"Actor Returned: {s}\n\n")

    //|> Source.runForEach mat (fun s -> actor2ref <! s)
    |> Source.toMat (Sink.forEach (fun s -> actor2ref <! s)) Keep.left

    //|> Source.toMat Sink.ignore Keep.left
    |> Graph.run mat

s <! "Boo"


let s2: IActorRef<string> =
    Source.actorRef OverflowStrategy.DropNew 1000
    |> Source.mapMaterializedValue (spawnActor1)
    |> Source.toMat (Sink.forEach (fun s -> actor2ref <! s)) Keep.left
    |> Graph.run mat

s2 <! "Boo"

let s3: ISourceQueueWithComplete<string> =
    Source.queue OverflowStrategy.Backpressure 1000
    |> Source.toMat (Sink.forEach (fun s -> s2 <! s)) Keep.left
    |> Graph.run mat

// Function to send a message to the queue
let sendMessageToQueue (message: string) =
    async {
        let! result = s3.OfferAsync(message) |> Async.AwaitTask

        match result with
        | :? QueueOfferResult.Enqueued -> printfn "Message '%s' enqueued successfully." message
        | :? QueueOfferResult.Dropped -> printfn "Message '%s' was dropped." message
        | :? QueueOfferResult.QueueClosed -> printfn "Queue is closed. Cannot enqueue message '%s'." message
        | :? QueueOfferResult.Failure as ex -> printfn "Failed to enqueue message '%s': %A" message ex
        | _ -> false |> ignore
    }

// Example usage
s3.OfferAsync("aaa") |> Async.AwaitTask |> ignore
sendMessageToQueue "Hello, world!" |> Async.Start


type MyActor() =
    inherit ReceiveActor()

    do
        // Define the message handling logic
        base.Receive<string>(fun message -> printfn "Received message: %s" message)

let myActorRef = system.ActorOf<MyActor>("myActor")

let scheduler = system.Scheduler

scheduler.ScheduleTellRepeatedly(
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5),
    myActorRef,
    "Hello from scheduler",
    ActorRefs.NoSender
)

//myActorRef.Tell PoisonPill.Instance

system.Terminate() |> ignore

// http://api.getakka.net/docs/stable/html/3E6D3122.htm
// https://github.com/petabridge/akka-bootcamp/blob/master/src/Unit-2/lesson3/README.md


