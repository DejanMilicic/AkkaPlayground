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

type Event = { Name: string; Date: DateTime }

type Region = string

type RegionalEvent = { Event: Event; Region: Region }

type Subscriber = string

type RegionalEventSubscriber =
    { Event: Event
      Region: Region
      Subscriber: Subscriber }

let xMasDay =
    { Name = "Christmas"
      Date = new DateTime(2023, 12, 25) }

let xMasDay2 =
    { Name = "Christmas Day 2"
      Date = new DateTime(2023, 12, 26) }

let findRegions: Flow<Event, RegionalEvent seq, NotUsed> =
    Flow.Create<Event>()
    |> Flow.map (fun eventDay -> seq { { Event = eventDay; Region = "France" } })

let findSubscribers: Flow<RegionalEvent seq, RegionalEventSubscriber seq, NotUsed> =
    Flow.Create<RegionalEvent seq>()
    |> Flow.map (fun regionalEvents ->
        regionalEvents
        |> Seq.map (fun regionalEvent ->
            { Event = regionalEvent.Event
              Region = regionalEvent.Region
              Subscriber = "John Doe" }))

let scheduler (sendNotification) =
    Source.ofSeq [ xMasDay; xMasDay2 ]
    |> Source.via findRegions
    |> Source.via findSubscribers
    |> Source.toMat (Sink.forEach sendNotification) Keep.none
    |> Graph.runnable
    |> Graph.run mat

let sendNotification (subs: RegionalEventSubscriber seq) =
    subs
    |> Seq.iter (fun sub -> printfn $"Sending notification about event '{sub.Event.Name}' to '{sub.Subscriber}'")
    |> ignore

scheduler sendNotification |> ignore
