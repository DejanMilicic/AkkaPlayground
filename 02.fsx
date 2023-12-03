// Stateful actors

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

//-----------------------------------------------------
let accumulator =
    spawnAnonymous system
    <| props (fun mailbox ->
        let rec loop sum =
            actor {
                let! msg = mailbox.Receive()

                let newSum = sum + msg
                printfn $"Sum = {newSum}"

                return! loop newSum
            }

        loop 0)

accumulator <! -1
