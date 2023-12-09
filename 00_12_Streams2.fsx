// following https://getakka.net/articles/streams/quickstart.html
// and coding allong in Akkling

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams
open System.Linq
open System.Numerics
open Akka.Actor
open System.IO

let system = System.create "streams-sys" <| Configuration.defaultConfig ()
let mat = system.Materializer()

// print out first 100 numbers

Enumerable.Range(1, 100)
|> Source.From
|> Source.runForEach mat (fun x -> printfn $"{x}")

// calculate factorials 1-100 and store them to a file

Enumerable.Range(1, 100)
|> Source.From
|> Source.scan (fun acc next -> acc * bigint next) (bigint 1)
|> Source.skip 1 // since scan will output initial value as well
|> Source.map (fun x -> Akka.IO.ByteString.FromString(x.ToString() + "\n"))
|> Source.runWith mat (Sink.toFile "c:\\temp\\factorials.txt")

// calculating factorials into file, but this time with a custom sink

let lineSink filename source =
    source
    |> Source.map (fun x -> Akka.IO.ByteString.FromString(x.ToString() + "\n"))
    |> Source.toMat (FileIO.ToFile(new FileInfo(filename))) Keep.right

Enumerable.Range(1, 100)
|> Source.From
|> Source.scan (fun acc next -> acc * bigint next) (bigint 1)
|> Source.skip 1 // since scan will output initial value as well
|> lineSink "c:\\temp\\factorials.txt"
|> Graph.run mat

// time-based processing

let factorials =
    Enumerable.Range(1, 100)
    |> Source.From
    |> Source.scan (fun acc next -> acc * bigint next) (bigint 1)

factorials
|> Source.zipWith (Source.From(Enumerable.Range(0, 20))) (fun num idx -> $"{idx}! = {num}")
|> Source.throttle ThrottleMode.Shaping 1 1 (TimeSpan.FromSeconds(1.0))
|> Source.runForEach mat (fun x -> printfn $"{x}")

//================================================================================
