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

==============================================================================
==============================================================================

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