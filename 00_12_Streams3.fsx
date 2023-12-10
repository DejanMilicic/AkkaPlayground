// Akkling.Streams, reading from Coinbase

(*
https://www.piesocket.com/websocket-tester

https://docs.cloud.coinbase.com/exchange/docs/websocket-overview
wss://ws-feed-public.sandbox.exchange.coinbase.com
{"type":"subscriptions","channels":[{"name":"heartbeat","product_ids":["BTC-GBP"]}]}

*)
//==============================================================================

#r "System.Net.WebSockets"
#r "System.Threading"

open System
open System.Net.WebSockets
open System.Threading
open System.Text

let webSocketWork (uri: Uri) (message: string) =
    async {
        use clientWebSocket = new ClientWebSocket()

        do! clientWebSocket.ConnectAsync(uri, CancellationToken.None) |> Async.AwaitTask

        // Send a message
        let messageBytes = Encoding.UTF8.GetBytes(message)
        let sendBuffer = ArraySegment<byte>(messageBytes)

        do!
            clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None)
            |> Async.AwaitTask

        // Receive a message
        let receiveBuffer = ArraySegment<byte>(Array.zeroCreate 1024)

        let! receivedResult =
            clientWebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None)
            |> Async.AwaitTask

        let receivedMessage =
            Encoding.UTF8.GetString(receiveBuffer.Array, 0, receivedResult.Count)

        printfn "Received: %s" receivedMessage

        do!
            clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None)
            |> Async.AwaitTask
    }

let message =
    """{ "type": "subscribe", "product_ids": ["BTC-USD"], "channels": ["heartbeat"]}"""

let uri = Uri("wss://ws-feed-public.sandbox.exchange.coinbase.com")

webSocketWork uri message |> Async.RunSynchronously

//==============================================================================
//==============================================================================

#r "System.Net.WebSockets"
#r "System.Threading"

open System
open System.Net.WebSockets
open System.Threading
open System.Text

let webSocketWork (uri: Uri) (message: string) =
    async {
        use clientWebSocket = new ClientWebSocket()

        do! clientWebSocket.ConnectAsync(uri, CancellationToken.None) |> Async.AwaitTask

        // Send a message
        let messageBytes = Encoding.UTF8.GetBytes(message)
        let sendBuffer = ArraySegment<byte>(messageBytes)

        do! clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask

        let receiveBuffer = ArraySegment<byte>(Array.zeroCreate 1024)

        // Keep receiving messages
        let mutable keepReceiving = true
        while keepReceiving do
            let! receivedResult = clientWebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None) |> Async.AwaitTask
            let receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array, 0, receivedResult.Count)
            printfn "Received: %s" receivedMessage

            // Update keepReceiving based on some condition or message content
            // For example, to stop when a specific message is received:
            // keepReceiving <- not (receivedMessage.Contains("specific stop message"))

        do! clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None) |> Async.AwaitTask
    }

let message =
    """{ "type": "subscribe", "product_ids": ["BTC-USD"], "channels": ["heartbeat"]}"""

let uri = Uri("wss://ws-feed-public.sandbox.exchange.coinbase.com")

webSocketWork uri message |> Async.RunSynchronously

//==============================================================================
//==============================================================================


#r "System.Net.WebSockets"
#r "System.Threading"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"

open System
open System.Net.WebSockets
open System.Threading
open System.Text
open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams
open System.Linq
open System.Numerics
open Akka.Actor
open System.IO
open System
open System.Net.WebSockets
open System.Threading
open System.Text

let webSocketSource (uri: Uri) (message: string) =
    seq {
        use clientWebSocket = new ClientWebSocket()
        clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait()

        // Send a message
        let messageBytes = Encoding.UTF8.GetBytes(message)
        let sendBuffer = ArraySegment<byte>(messageBytes)
        clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None).Wait()

        let receiveBuffer = ArraySegment<byte>(Array.zeroCreate 1024)
        let mutable shouldContinue = true

        while shouldContinue && not clientWebSocket.CloseStatus.HasValue do
            let result = clientWebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None).Result
            if result.MessageType = WebSocketMessageType.Close then
                shouldContinue <- false
            else
                let receivedMessage = Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count)
                yield receivedMessage

        clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None).Wait()
    }

let message = """{ "type": "subscribe", "product_ids": ["BTC-USD"], "channels": ["heartbeat"]}"""
let url = Uri("wss://ws-feed-public.sandbox.exchange.coinbase.com")

// Example usage with Akka Stream (or Akkling Streams in F#)

let system = System.create "streams-sys" <| Configuration.defaultConfig ()
let mat = system.Materializer()

Source.From(webSocketSource url message)
|> Source.runForEach mat (fun x -> printfn $"{x}")

//let source = 
//source.To(Sink.ForEach(fun msg -> printfn "Received: %s" msg)).Run(materializer)

//==============================================================================
// alrternative versions below
(*
let webSocket = WebSocket.CreateClientWebSocket("wss://ws-feed-public.sandbox.exchange.coinbase.com")


let message = "{\"type\":\"subscriptions\",\"channels\":[{\"name\":\"heartbeat\",\"product_ids\":[\"BTC-GBP\"]}]}"

let clientWebSocketEcho () = 
    async {
        use client = new ClientWebSocket()
        do! client.ConnectAsync(Uri("wss://ws-feed-public.sandbox.exchange.coinbase.com"), CancellationToken.None) |> Async.AwaitTask

        let buffer = Encoding.UTF8.GetBytes(message)
        let! result = client.ReceiveAsync(ArraySegment(buffer), CancellationToken.None) |> Async.AwaitTask

        while not result.CloseStatus.HasValue do
            do! client.SendAsync(ArraySegment(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None) |> Async.AwaitTask
            let! newResult = client.ReceiveAsync(ArraySegment(buffer), CancellationToken.None) |> Async.AwaitTask
            //printf "Received: %s" (Encoding.UTF8.GetString(newResult, 0, newResult.Count))
            printf "received!!!\n"
            ()

        //do! client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None) |> Async.AwaitTask
    }

clientWebSocketEcho() |> Async.RunSynchronously
*)

//==============================================================================

