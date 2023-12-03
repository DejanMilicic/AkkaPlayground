// Stateful actors

#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akka.Actor

let system = System.create "my-system" <| Configuration.defaultConfig ()

//=====================================================
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

accumulator <! 1

//=====================================================

let accumulator2Behavior (mailbox: Actor<int>) =
    let rec loop sum =
        actor {
            let! msg = mailbox.Receive()

            let newSum = sum + msg
            printfn $"Sum = {newSum}"

            return! loop newSum
        }

    loop 0

let accumulator2 = spawnAnonymous system (props accumulator2Behavior)

accumulator2 <! 1

//=====================================================

let accumulator3Behavior (initState: int) (mailbox: Actor<int>) =
    let rec loop sum =
        actor {
            let! msg = mailbox.Receive()

            let newSum = sum + msg
            printfn $"Sum = {newSum}"

            return! loop newSum
        }

    loop initState

let accumulator3 = spawnAnonymous system (props (accumulator3Behavior 555))

accumulator3 <! 1
