#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akkling
open Akka.Actor
open Akka.Streams
open Akka.Streams.Dsl
open Akkling.Streams
open System
open Akka

let system = System.create "scheduler" <| Configuration.defaultConfig ()

let mat = system.Materializer()

//=================================================

type EventDay = DateTime

type Region = string

type RegionalEvent = { EventDay: EventDay; Region: Region }

type Subscriber = string

type RegionalEventSubscriber =
    { EventDay: EventDay
      Region: Region
      Subscriber: Subscriber }

let xMasDay = new DateTime(2023, 12, 25)
let xMasDay2 = new DateTime(2023, 12, 26)

let findRegions: Flow<EventDay, RegionalEvent seq, NotUsed> =
    Flow.Create<EventDay>()
    |> Flow.map (fun eventDay ->
        seq {
            { EventDay = eventDay
              Region = "France" }
        })

let findSubscribers: Flow<RegionalEvent seq, RegionalEventSubscriber seq, NotUsed> =
    Flow.Create<RegionalEvent seq>()
    |> Flow.map (fun regionalEvents ->
        regionalEvents
        |> Seq.map (fun regionalEvent ->
            { EventDay = regionalEvent.EventDay
              Region = regionalEvent.Region
              Subscriber = "John Doe" }))


let mySink = Sink.forEach (fun (x: RegionalEventSubscriber seq) -> 
    x |> Seq.iter (fun (x: RegionalEventSubscriber) -> 
        printfn "%A" x))

Source.ofSeq [ xMasDay; xMasDay2 ]
|> Source.via findRegions
|> Source.via findSubscribers
|> Source.toSink mySink
|> Graph.runnable
|> Graph.run mat
