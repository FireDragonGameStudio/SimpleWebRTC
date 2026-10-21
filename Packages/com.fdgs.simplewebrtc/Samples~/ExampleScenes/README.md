# Example Scenes

The classic SimpleWebRTC samples: clients that find each other through a **WebSocket signaling server** and then exchange video, audio and data channel messages peer-to-peer. Start here if you want to connect separate apps or devices.

| Scene | What it shows |
|-------|---------------|
| `WebSocket-TestConnection` | Only the signaling connection, without WebRTC. Use it to check that your server works. |
| `WebRTC-SingleClient-STUNConnection` | One client with buttons for every feature. Run it several times to connect the instances. |
| `WebRTC-SingleClient-wLobby-STUNConnection` | The same client driven from C# (`SimpleLobbyManager`), with a small lobby chat. |
| `WebRTC-MultipleClients-STUNConnection` | Three clients (Red, Green, Blue) in one scene, connected as a full mesh. |

## Requirements
- SimpleWebRTC 2.x with [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) 3.2.0+
- [NativeWebSocket](https://github.com/endel/NativeWebSocket) 2.x (`https://github.com/endel/NativeWebSocket.git#upm-2`), or the Meta Voice SDK (see below)
- A running signaling server (see below)

## 1. Start the signaling server
All scenes connect to `ws://localhost:3000`. The package contains a minimal Node.js relay server in the folder `Server~`. If you installed the package via git URL, it's at `Library/PackageCache/com.fdgs.simplewebrtc@<hash>/Server~`. The same code is also in `Runtime/SignalServerCode.txt`.

```sh
cd Server~
npm install
npm start
```

The server prints `SimpleWebRTC signaling server listening on ws://localhost:3000`. It only relays every message to all other connected clients, so any WebSocket relay works the same way.

**Other devices:** `localhost` only works on the machine that runs the server. For phones, headsets or other PCs, set the server URL on the `WebSocketSignaling` component to the IP of that machine, e.g. `ws://192.168.1.10:3000`, and allow port 3000 in its firewall. For production, host the server somewhere with TLS and use `wss://your-host`.

**Meta Voice SDK instead of NativeWebSocket:** remove the `WebSocketSignaling` component, add `MetaWebSocketSignaling` with the same server URL, and assign it as **Signaling** on the `WebRTCPeerManager`.

## 2. Running several clients
WebRTC needs at least two peers. You can:
- **build the scene** (File > Build Profiles, add the scene) and run the player next to the Editor,
- run **several builds** on the same or different machines,
- use Unity's [Multiplayer Play Mode](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@latest) package to simulate extra players inside the Editor,
- or use the `WebRTC-MultipleClients-STUNConnection` scene, which already has three clients.

Every client gets a unique peer id automatically: the **Local Peer Id** of the `WebRTCPeerManager` plus a random suffix.

---

## WebSocket-TestConnection
Tests the signaling channel alone, without WebRTC.

- **Object:** `ConnectionTest`, with `WebSocketSignaling` and `TestWebSocketConnection`.
- **What happens:** on Play it connects to the server and sends `plain text message from <device> at <time>` every second. It logs everything it receives.
- **Try it:** run two instances. Each one logs the messages of the other in the Console. If nothing arrives, check the server window and the server URL.

## WebRTC-SingleClient-STUNConnection
A complete client with a button for every action. This is the best scene to get a feel for the components.

**Client object `Client-STUNConnection`**, an instance of the prefab `Runtime/Prefabs/Client-STUNConnection.prefab`:

| Component | Setting |
|-----------|---------|
| `WebSocketSignaling` | server `ws://localhost:3000` |
| `WebRTCPeerManager` | peer id `Peer` + random suffix, room `default`, Google STUN server, VP8 |
| `DataChannel` | label `data`, channel id 0, reliable |
| `CameraVideoSource` + `VideoSender` | streams `Client-StreamingCamera` (1280×720, looking at a rotating cube with a random color), local preview in `Client-Streaming-RawImage` |
| `AudioSender` | streams the `Client-Streaming-AudioSource` (forest ambience) |
| `VideoReceiver` + `RawImageVideoSink` | spawns one RawImage per remote peer under `Client-ReceivingRawImages` |
| `AudioReceiver` | spawns one AudioSource per remote peer under `Client-AudioReceiver` |
| `WebRTCStatsMonitor` | connection statistics, overlay off |

**Buttons:**

| Button | Calls |
|--------|-------|
| Connect / Disconnect | `WebRTCPeerManager.Connect()` / `Disconnect()` |
| DataChannel Message | `DataChannel.Send("Test Message Over DataChannel")` |
| Start / Stop Video | `VideoSender.StartStreaming()` / `StopStreaming()` |
| Start / Stop Audio | `AudioSender.StartStreaming()` / `StopStreaming()` |

**Try it:**
1. Start the server and two clients, then press **Connect** in both. The Console shows `connection state Connected`.
2. Press **Start Video** in one client. Its cube appears in the other client. Press **Stop Video** and it disappears again.
3. Press **DataChannel Message** and watch the Console of the other client (data channel logs are enabled).
4. Select `Client-STUNConnection` in Play mode, enable **Show Overlay** on the `WebRTCStatsMonitor` (or press its *Toggle Overlay* button), and watch bitrate, FPS and round trip time.
5. Start a third client later. It automatically receives the streams that are already running.

## WebRTC-SingleClient-wLobby-STUNConnection
The same client, but the UI is wired up in code instead of in the inspector. Read `Runtime/Scripts/Utils/SimpleLobbyManager.cs` to see how to use SimpleWebRTC from C#.

- **Join Lobby** calls `peerManager.Connect()`. When the signaling state turns `Connected`, the lobby buttons appear.
- When a data channel to a new player opens (`DataChannel.ChannelOpened`), the lobby sends that player a greeting with `SendTo(peerId, ...)`.
- **Send Chat Message** broadcasts with `chatChannel.Send(...)`, and received messages are appended to the chat text (`DataChannel.MessageReceived`).
- **Start/Stop Video/Audio** call the `VideoSender` and `AudioSender`.
- **Leave Lobby** sends a goodbye message and calls `peerManager.Disconnect()`.

The player name is the **Player Name** field of `SimpleLobbyManager` plus the device name (plus the random suffix of the peer manager).

## WebRTC-MultipleClients-STUNConnection
Three complete clients, `Red-STUNConnection`, `Green-STUNConnection` and `Blue-STUNConnection`, in one scene. Each has its own camera and cube, and all of them talk to the same signaling server. A client connects as soon as you activate it, because *Connect On Start* is enabled.

**Try it:**
1. Start the server and press Play.
2. In the Hierarchy, activate `Red-STUNConnection`, `Green-STUNConnection` and `Blue-STUNConnection`. They connect to each other as a full mesh (3 connections).
3. Select a client and press **Start Streaming** on its `VideoSender` or `AudioSender` (these buttons appear in Play mode). The other two clients show the video in their panel and play the audio.
4. Press **Send Test Message** on a `DataChannel` and check the Console.
5. Deactivate one client: the others close the connection to it (`closed session to ...`). Activate it again and it reconnects.

This scene is useful for testing and for seeing how multiple peer managers work side by side. In a real app, every device runs only one client.

## Troubleshooting
- **Signaling stays `Connecting`:** the server isn't running, or the URL or firewall is wrong. Test with `WebSocket-TestConnection`.
- **Peers connect via signaling, but WebRTC never reaches `Connected`:** the devices can't reach each other directly, for example on different networks behind strict firewalls. Add a TURN server to **Ice Servers** on the `WebRTCPeerManager`.
- **Peers don't see each other at all:** check that all clients use the same server URL and the same **Room**.
- **No video:** make sure the camera of the `CameraVideoSource` renders something. The sender shows its own video in the preview RawImage.
