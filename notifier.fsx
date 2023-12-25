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

type Event = { Name: string; Date: DateOnly }

type Region = string

type RegionalEvent = { Event: Event; Region: Region }

type Subscriber = string

type RegionalEventSubscriber =
    { Event: Event
      Region: Region
      Subscriber: Subscriber }

let notifier
    (regionalEventsFinder: DateOnly -> RegionalEvent seq)
    (subscribersFinder: RegionalEvent seq -> RegionalEventSubscriber seq)
    (notificationsSender: RegionalEventSubscriber seq -> unit)
    (events: DateOnly seq)
    =
    Source.ofSeq events
    |> Source.via (Flow.Create<DateOnly>() |> Flow.map regionalEventsFinder)
    |> Source.via (Flow.Create<RegionalEvent seq>() |> Flow.map subscribersFinder)
    |> Source.toMat (Sink.forEach notificationsSender) Keep.none
    |> Graph.runnable
    |> Graph.run mat

//===============================================

let regionalEventsFinder (date: DateOnly) : RegionalEvent seq =
    seq {
        match date with
        | _ when date.Day = 25 && date.Month = 12 ->
            { Event = { Name = "Xmas"; Date = date }
              Region = "France" }

            { Event = { Name = "Xmas"; Date = date }
              Region = "Germany" }
        | _ when date.Day = 26 && date.Month = 12 ->
            { Event = { Name = "Xmas Day 2"; Date = date }
              Region = "Sweden" }
    }

let subscribersFinder (regionalEvents: RegionalEvent seq) : RegionalEventSubscriber seq =
    regionalEvents
    |> Seq.map (fun regionalEvent ->
        { Event = regionalEvent.Event
          Region = regionalEvent.Region
          Subscriber = "John Doe" })

let notificationsSender (subs: RegionalEventSubscriber seq) : unit =
    subs
    |> Seq.iter (fun sub ->
        printfn $"Sending notification about event '{sub.Event.Name} @ {sub.Region}' to '{sub.Subscriber}'")
    |> ignore

let noty: (DateOnly seq -> unit) =
    notifier regionalEventsFinder subscribersFinder notificationsSender

//================================================

[ new DateOnly(2023, 12, 25); new DateOnly(2023, 12, 26) ] |> noty |> ignore
