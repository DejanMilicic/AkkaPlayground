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

let queue: ISourceQueueWithComplete<string> =
    Source.queue OverflowStrategy.Backpressure 1000
    |> Source.toMat (Sink.forEach (fun s -> s2 <! s)) Keep.left
    |> Graph.run mat

// Function to send a message to the queue
let sendMessageToQueue (message: string) =
    async {
        let! result = queue.OfferAsync(message) |> Async.AwaitTask

        match result with
        | :? QueueOfferResult.Enqueued -> printfn "Message '%s' enqueued successfully." message
        | :? QueueOfferResult.Dropped -> printfn "Message '%s' was dropped." message
        | :? QueueOfferResult.QueueClosed -> printfn "Queue is closed. Cannot enqueue message '%s'." message
        | :? QueueOfferResult.Failure as ex -> printfn "Failed to enqueue message '%s': %A" message ex
        | _ -> false |> ignore
    }

// Example usage
queue.OfferAsync("aaa") |> Async.AwaitTask |> ignore
//queue.AsyncOffer("aaa") |> Async.AwaitTask |> ignore
//queue.AsyncOffer("xxx") |!> (typed ActorBase.Context.Self)
sendMessageToQueue "Hello, world!" |> Async.Start

//====================================================

let queuingActor (queue: ISourceQueueWithComplete<string>) (m: Actor<string>) =
    let rec loop () =
        actor {
            let! msg = m.Receive()
            printf "\n queuingActor Received: %s\n\n" msg

            queue.OfferAsync(msg) |> Async.AwaitTask |> ignore

            return! loop ()
        }

    loop ()

let queuingActorRef queue =
    spawnAnonymous system <| props (queuingActor queue)

let myQ: ISourceQueueWithComplete<string> =
    Source.queue OverflowStrategy.Backpressure 1000
    //|> Source.toMat Sink.ignore Keep.left
    |> Source.toMat (Sink.forEach (fun s -> printfn $"\n Received in the QUEUE : {s}")) Keep.left
    |> Graph.run mat

let xxx = queuingActorRef myQ

xxx <! "Hello, world!"

//====================================================

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

//===========================================



//===========================================
(*
- table with aggregated events [event name, event description, submit date, event target date]
- every day, fetch all events that are not passed yet
    - for each event fetch regions
    - find all subscribers who wants to be notified exactly N days before event target date
        (target date - today = K)
    - find users intersted in a specific region and would like to be notified exactly K days before event target date
    - another table with users and theirt notification preferences
    - send one email per : one user, region, event, date

- notification can be email, sms, ....

- 1-30 days of notifications ahead

- Q: what happens with newly registered users?
- Q: what about immediate events, e.g. "Queen is dead"?
    - they should be sent immediately

Implementation
- two source actors
- Actor1: immediate (CQRS immediate events)
- Actor2: scheduled (CQRS scheduled events)
- Stream.merge of these two streams
- no need to persist anything

Implementation plan
- create two actors
- mock immediate one
- holiday actor
    - read configuration
    - emit event
    - same actor will pick it up
        - retrieve all the events, not passed yet
        - for each event
            - retrieve all regions
            - for each region
                - retrieve all subscribers, where
                    - date of interest should be exactly K days, meaning "today" is the date to send notification
                    - send notification

possible implementation
- actor1 with duty to send notification
- actor2 that would resolve subscriber from region (receives event and region)
- actor3 that would fetch events and regions

scheduler |> actor3 |> actor2 |> actor1

immediate_actor |> actor2

study: https://getakka.net/articles/streams/integration.html


*)
