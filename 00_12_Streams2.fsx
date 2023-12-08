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


let system = System.create "streams-sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

let source = [| 1; 2; 3; 4; 5 |] |> Source.From

let source2 = Enumerable.Range(1, 100) |> Source.From

Enumerable.Range(1, 100)
|> Source.From
|> Source.runForEach mat (fun x -> printfn $"{x}")

//==========================================================

Enumerable.Range(1, 100)
|> Source.From
|> Source.scan (fun acc next -> acc * bigint next) (bigint 1)
|> Source.map (fun x -> Akka.IO.ByteString.FromString(x.ToString() + "\n"))
|> Source.runWith mat (Sink.toFile "c:\\temp\\factorials.txt")
