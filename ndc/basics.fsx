#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open Akkling
open Akkling.Streams

let system = System.create "sys" <| Configuration.defaultConfig ()

let mat = system.Materializer()

Source.ofList [ 1..10 ]
|> Source.map ((+) 1)
|> Source.map ((+) 1)
|> Source.map ((+) 1)
|> Source.runForEach mat (printfn "%A")
|> Async.RunSynchronously
