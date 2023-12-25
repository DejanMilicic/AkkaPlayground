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

let xMasDay = new DateTime(2023, 12, 25)
let xMasDay2 = new DateTime(2023, 12, 26)

let findRegions: Flow<EventDay, RegionalEvent, NotUsed> =
    Flow.Create<EventDay>()
    |> Flow.map (fun eventDay ->
        { EventDay = eventDay
          Region = "France" })

let mySink = Sink.forEach (fun (x: RegionalEvent) -> printfn "woohoo")

Source.ofSeq [ xMasDay; xMasDay2 ]
|> Source.via findRegions
|> Source.toSink mySink
|> Graph.runnable
|> Graph.run mat
