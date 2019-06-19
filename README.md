# RxSocks
WebSocket api buid on top of vanilla websocket and websocket client, giving async functionalities

# Download
Library is avaliable in Nuget. For download use:

` Install-Package RxSocks -Version 1.0.2 `

# Functionality 

Addition to .net Websocket client, it support http-headers like setting userAgent

# Usage

MessagePack support is in-built.

Create client:

` var client = new MessagePackSocketClient<MsgPackRequest, MsgPackResponse>() `

Optional pass you custom deserializer in the constructor, by default it will use `MessagePackSerializer.Deserializer<MsgPackResponse>(..)`

**Consuming client:**

Async:

`client.GetResponseAsync(request)`

Streaming/Observable:

`client.GetObservable(request)`
