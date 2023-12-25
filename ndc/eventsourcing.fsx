#r "nuget: Akkling"
#r "nuget: Akkling.Streams"
#r "nuget: Akka.Persistence"
#r "nuget: Akkling.Persistence"

open Akka.IO
open Akkling
open Akkling.Streams
open Akkling.Streams.Operators
open Akka.Streams.Dsl
open Akka.Streams
open Akkling.Persistence
open System
open Akka.Persistence.Journal

let sys = System.create "sys" <| Configuration.defaultConfig ()

let mat = sys.Materializer()

type OrderItem = string

type ItemOrdered = string * DateTime

type CustomerMsg =
    | OrderItem of OrderItem
    | ItemOrdered of ItemOrdered

let pref =
    spawn sys "customer-1"
    <| propsPersist (fun ctx ->
        let rec customer state =
            actor {
                let! msg = ctx.Receive()

                match msg with
                | OrderItem item -> return Persist(ItemOrdered(item, DateTime.UtcNow))
                | ItemOrdered item -> return! customer (item :: state)
            }

        customer [])

let sec1 = TimeSpan.FromSeconds 1.

let cancellable =
    Source.Tick(sec1, sec1, 1)
    |> Source.scan (+) 0
    |> Source.map string
    |> Source.map OrderItem
    |> Source.toMat (Sink.forEach ((<!) pref)) Keep.left
    |> Graph.run mat

//let queries = sys.ReadJournalFor<MemoryJournal>  .<SqlReadJournal>("akka.persistence.query.journal.sql-read-journal")
