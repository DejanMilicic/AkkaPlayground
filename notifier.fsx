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

let scheduler
    (regionsFinder: Event -> RegionalEvent seq)
    (subscribersFinder: RegionalEvent seq -> RegionalEventSubscriber seq)
    (notificationsSender: RegionalEventSubscriber seq -> unit)
    =
    Source.ofSeq [ xMasDay; xMasDay2 ]
    |> Source.via (Flow.Create<Event>() |> Flow.map regionsFinder)
    |> Source.via (Flow.Create<RegionalEvent seq>() |> Flow.map subscribersFinder)
    |> Source.toMat (Sink.forEach notificationsSender) Keep.none
    |> Graph.runnable
    |> Graph.run mat

//===============================================

let regionsFinder (event: Event) : RegionalEvent seq =
    seq { { Event = event; Region = "France" } }

let subscribersFinder (regionalEvents: RegionalEvent seq) : RegionalEventSubscriber seq =
    regionalEvents
    |> Seq.map (fun regionalEvent ->
        { Event = regionalEvent.Event
          Region = regionalEvent.Region
          Subscriber = "John Doe" })

let notificationsSender (subs: RegionalEventSubscriber seq) : unit =
    subs
    |> Seq.iter (fun sub -> printfn $"Sending notification about event '{sub.Event.Name}' to '{sub.Subscriber}'")
    |> ignore

scheduler regionsFinder subscribersFinder notificationsSender |> ignore
