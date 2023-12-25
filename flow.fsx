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

// Step 1: Define a Source
let mySource = Source.ofList [ 1..10 ]

// Step 2: Define a Flow
let myFlow: Flow<int, int, NotUsed> =
    Flow.Create<int>() |> Flow.map (fun x -> x * 2) // Example: Doubling each element

// Step 3: Connect Source to Flow using via
let sourceViaFlow = mySource.Via(myFlow)

// Step 4: Define a Sink (Optional)
let mySink = Sink.forEach (fun x -> printfn "%d" x)

// Step 5: Run the Stream
sourceViaFlow.To(mySink) |> Graph.runnable |> Graph.run mat

//=================================================

Source.ofList [ 1..10 ]
|> Source.via myFlow
|> Source.toSink mySink
|> Graph.runnable
|> Graph.run mat

//=================================================
