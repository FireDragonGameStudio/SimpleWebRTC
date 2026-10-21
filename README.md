![simple-webrtc-logo](https://github.com/user-attachments/assets/4a243ff7-8260-4277-8d7a-844f67ffdcff)

# SimpleWebRTC
SimpleWebRTC is a component-based Unity wrapper for peer-to-peer audio, video, 360° video and data communication over WebRTC. It builds on [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs), the community-maintained fork of Unity's WebRTC package.

Version 2.0 splits the former all-in-one `WebRTCConnection` into small components. Add only what you need, swap the signaling backend, or write your own video sources, sinks and transports without touching the WebRTC core.

## Features
- Full-mesh peer-to-peer connections with automatic ("perfect") negotiation. Peers can join late and start or stop streams at any time.
- Pluggable signaling: WebSocket ([NativeWebSocket](https://github.com/endel/NativeWebSocket) or the Meta Voice SDK version), Photon Fusion 2, in-process loopback, or your own transport.
- Rooms, so several apps can share one signaling server.
- Data channels (text and binary, reliable or unreliable, several channels per connection).
- Video from cameras, textures, webcams or as 360° video (mono or stereo over-under).
- Received video shown in RawImages, on renderers or in RenderTextures.
- Audio from AudioSources or the microphone, played back per peer.
- STUN and TURN servers, preferred video codec, bitrate and framerate limits.
- Connection statistics per peer (round trip time, bitrates, FPS, resolution, codecs, packet loss, jitter, direct or TURN-relayed path), with an optional on-screen overlay.
- Unity Visual Scripting nodes and events.
- Migration tool for SimpleWebRTC 1.x scenes.

## Requirements
- Unity 6000.0 or newer
- [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) `com.fdgs.webrtc` 3.2.0 or newer
- For WebSocket signaling, one of:
  - [NativeWebSocket](https://github.com/endel/NativeWebSocket) 2.x (`https://github.com/endel/NativeWebSocket.git#upm-2`)
  - the Meta Voice SDK (`com.meta.xr.sdk.voice`), which already contains a NativeWebSocket version
- Optional: Photon Fusion 2, Unity Visual Scripting

UPM packages cannot depend on git packages, so install WebRTC (FDGS) and NativeWebSocket yourself. Without WebRTC (FDGS), SimpleWebRTC is not compiled and offers to install it. Tools > SimpleWebRTC > Check Dependencies shows what is available.

## Installation
### Unity Package Manager
1. Open the Package Manager, click on the + sign and select "Add package from git URL".
2. Add `https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs.git#v3.2.0`
3. Add `https://github.com/endel/NativeWebSocket.git#upm-2` (skip this if the Meta Voice SDK is installed)
4. Add `https://github.com/FireDragonGameStudio/SimpleWebRTC.git?path=/Assets/SimpleWebRTC`

### Unity Asset Store
Install [SimpleWebRTC](https://assetstore.unity.com/packages/tools/network/simplewebrtc-309727) from the Asset Store and add the dependencies above.

### Signaling server
SimpleWebRTC only needs a WebSocket server that relays every message to all other connected clients. A ready-to-use Node.js server is in `Server~` (also in `Runtime/SignalServerCode.txt`):
```sh
cd Server~
npm install
npm start   # ws://localhost:3000, set PORT to change it
```
Host it anywhere that supports WebSockets, then use `wss://your-host` as the server URL.

## Architecture
```
WebRTCPeerManager ── one PeerSession (RTCPeerConnection) per remote peer
 ├─ SignalingTransport   WebSocketSignaling | MetaWebSocketSignaling | PhotonFusionSignaling | LoopbackSignaling | your own
 ├─ DataChannel          any number, each with its own channel id
 ├─ VideoSender ── VideoSource      CameraVideoSource | TextureVideoSource | WebCamVideoSource | CubemapVideoSource | your own
 ├─ VideoReceiver ── VideoSink      RawImageVideoSink | RendererVideoSink | RenderTextureVideoSink | your own
 ├─ AudioSender                     AudioSource or microphone
 ├─ AudioReceiver                   one AudioSource per peer
 └─ WebRTCStatsMonitor              connection statistics per peer
```
All feature components find the `WebRTCPeerManager` on their GameObject or a parent. Put them on one GameObject, or on children for a cleaner hierarchy.

### Quick start
1. Create a GameObject and add `WebRTCPeerManager` and `WebSocketSignaling`. Assign the signaling component to the manager and set the server URL.
2. Add a `DataChannel`.
3. To send video, add a `CameraVideoSource` (assign a camera) and a `VideoSender` (assign the source).
4. To receive video, add a `RawImageVideoSink` (assign a RawImage or a parent for spawned images) and a `VideoReceiver` (add the sink to its list).
5. For audio, add an `AudioSender` (assign an AudioSource) and an `AudioReceiver`.
6. Call `Connect()` on the manager, or enable Connect On Start. In Play mode the inspector shows buttons for Connect, Start Streaming, Send Test Message and so on.

```csharp
public class MyClient : MonoBehaviour {
    [SerializeField] private WebRTCPeerManager peerManager;
    [SerializeField] private DataChannel chat;
    [SerializeField] private VideoSender video;

    private void Start() {
        peerManager.PeerConnected.AddListener(peerId => Debug.Log($"{peerId} connected"));
        chat.PeerMessageReceived += (peerId, message) => Debug.Log($"{peerId}: {message}");
        peerManager.Connect();
    }

    public void SayHello() => chat.Send("Hello everyone!");
    public void SayHelloTo(string peerId) => chat.SendTo(peerId, "Hello you!");
    public void ToggleVideo() => video.ToggleStreaming();
}
```

### Components
| Component | Purpose | Main API |
|-----------|---------|----------|
| `WebRTCPeerManager` | Joins a room over the signaling transport and manages one peer connection per remote peer. | `Connect()`, `Disconnect()`, `SetLocalPeerId()`, `SetRoom()`, `SetSignaling()`, `Sessions`, `LocalPeerId`; events `SignalingStateChanged`, `PeerJoined`, `PeerConnected`, `PeerLeft`, `SessionCreated`, `SessionClosed` |
| `WebSocketSignaling` / `MetaWebSocketSignaling` | Signaling over a WebSocket relay server (NativeWebSocket or Meta Voice SDK). | `ServerUrl` |
| `PhotonFusionSignaling` + `PhotonSignalingRelay` | Signaling through a running Photon Fusion 2 session. | |
| `LoopbackSignaling` | In-process signaling for peers in the same app, e.g. tests. | `Channel` |
| `DataChannel` | Pre-negotiated data channel to every peer. All peers need the same channel id. | `Send()`, `SendTo()` (string or byte[]); events `ChannelOpened`, `ChannelClosed`, `MessageReceived`, `PeerMessageReceived`, `BytesReceived` |
| `VideoSender` | Streams a `VideoSource` to all peers, including peers that join later. | `StartStreaming()`, `StopStreaming()`, `ToggleStreaming()`; bitrate and framerate limits |
| `VideoReceiver` | Forwards remote videos to its `VideoSink`s. | events `VideoStarted`, `VideoStopped`, `VideoUpdated`, `VideoRemoved` |
| `AudioSender` | Streams an AudioSource or the microphone. | `StartStreaming()`, `StopStreaming()` |
| `AudioReceiver` | Plays every peer's audio through its own AudioSource (optional prefab for spatial audio or mixer groups). | `TryGetAudioSource()`; events `AudioStarted`, `AudioStopped` |
| `WebRTCStatsMonitor` | Polls connection statistics for every peer at a fixed interval. Optional on-screen overlay (IMGUI) for debugging. | `Stats`, `TryGetStats()`, `Summary`, `ShowOverlay`; events `StatsUpdated` (`PeerStats`), `StatsTextUpdated` (formatted text, e.g. for a TextMeshPro text) |

### Extending SimpleWebRTC
- **Signaling backend:** derive from `SignalingTransport` and implement `Connect`, `Disconnect` and `Send`. Report state changes with `SetState` and incoming messages with `ReceiveMessage`; both are safe to call from any thread. The Custom Signaling Transport sample shows a complete example (`UdpBroadcastSignaling`, LAN without server) and a minimal template.
- **Video source:** derive from `VideoSource` and create a `VideoStreamTrack` in `CreateTrack`.
- **Video display:** derive from `VideoSink` and implement `SetVideo` / `RemoveVideo`.
- **Anything per peer:** derive from `PeerSessionComponent` and use `OnSessionCreated` / `OnSessionClosed` to add tracks, transceivers or data channels to every `PeerSession`.

### Connection statistics
Add a `WebRTCStatsMonitor` next to the `WebRTCPeerManager`. Every update interval (default 1 second) it reports for each peer:
- the round trip time and the available outgoing bandwidth of the active network path,
- whether the path is direct (host/srflx) or relayed through a TURN server, and the protocol,
- total send and receive bitrate,
- per audio and video stream: direction, codec and encoder/decoder (e.g. VP8 / libvpx, H264 / NvCodec), bitrate, resolution, frame rate, packet loss, jitter, jitter buffer delay, dropped frames, freezes, and why the encoder limits quality (bandwidth or CPU),
- data channel messages and bitrate.

```csharp
monitor.StatsUpdated += stats => {
    Debug.Log($"{stats.PeerId}: {stats.RoundTripTimeMs:0} ms, {stats.ReceiveBitrateKbps:0} kbps in");
    foreach (var stream in stats.Streams.Where(s => s.IsVideo)) {
        Debug.Log($"{stream.Direction} {stream.Codec} {stream.Width}x{stream.Height} @ {stream.FramesPerSecond:0} fps");
    }
};
```
Enable Show Overlay (or use the Toggle Overlay button in Play mode) for a quick on-screen view, or wire `StatsTextUpdated` to the `text` of a UI text.

### Signaling protocol
Messages are JSON objects, for example `{"v":2,"type":"offer","room":"default","from":"Peer-1a2b","to":"Peer-3c4d","payload":"..."}`. The types are `join`, `welcome`, `leave`, `offer`, `answer` and `candidate`. `to` is `*` for broadcasts. The server never needs to read the messages.

### Photon Fusion 2
1. Install Photon Fusion 2 and import the Photon Fusion sample.
2. Add the `SimpleWebRTC.PhotonFusion` assembly to NetworkProjectConfig > Assemblies To Weave.
3. Put a `PhotonSignalingRelay` on a NetworkObject and a `PhotonFusionSignaling` next to your `WebRTCPeerManager`, and reference them from each other.
4. Start the Fusion session, then call `Connect()` on the manager. Signaling connects as soon as the relay is spawned.

### Visual Scripting
1. Install [Unity Visual Scripting](https://docs.unity3d.com/Packages/com.unity.visualscripting@latest) and import the Visual Scripting sample.
2. Regenerate the nodes under Edit > Project Settings > Visual Scripting.
3. Add `WebRTCConnectionVisualScriptingEvents` to the client GameObject, which has the `WebRTCPeerManager`.
4. Add that GameObject as scene variable `ClientConnection`, see the sample scene.

## Migrating from SimpleWebRTC 1.x
2.0 is a breaking release, and the signaling protocol changed, so 1.x and 2.x clients cannot talk to each other.
1. Update the dependencies: replace `com.unity.webrtc` with `com.fdgs.webrtc`, and use NativeWebSocket 2.x.
2. Open each scene that uses `WebRTCConnection` or `WebRTCConnection_Generic` and run Tools > SimpleWebRTC > Migrate Open Scenes (or use the button in the old component's inspector). The tool:
   - creates the new components with your settings (server URL, STUN server, peer id, immersive setup, cameras, audio sources, receiving parents),
   - moves the UnityEvent listeners to the matching new events,
   - re-targets UnityEvent calls (e.g. buttons) from the old component to the new ones,
   - assigns empty references of other scripts when the scene has a single client.
3. Photon scenes need Photon Fusion 2 installed before migrating.
4. If the old component is on a prefab instance, the new components are added as overrides. Use Overrides > Apply All to update the prefab.
5. Update your scripts using this mapping:

| 1.x (`WebRTCConnection`) | 2.x |
|--------------------------|-----|
| `Connect()` / `ConnectWebRTC()` | `WebRTCPeerManager.Connect()`, WebRTC connects automatically |
| `Disconnect()` | `WebRTCPeerManager.Disconnect()` |
| `SetUniquePlayerName()` | `WebRTCPeerManager.SetLocalPeerId()` |
| `SendDataChannelMessage()` / `SendDataChannelMessageToPeer()` | `DataChannel.Send()` / `DataChannel.SendTo()` |
| `Start/StopVideoTransmission()` | `VideoSender.StartStreaming()` / `StopStreaming()` |
| `Start/StopAudioTransmission()` | `AudioSender.StartStreaming()` / `StopStreaming()` |
| `WebSocketConnectionChanged` | `WebRTCPeerManager.SignalingStateChanged` |
| `WebRTCConnected` | `WebRTCPeerManager.PeerConnected` |
| `DataChannelConnected` / `DataChannelMessageReceived` | `DataChannel.ChannelOpened` / `MessageReceived` |
| `VideoTransmissionReceived` / `AudioTransmissionReceived` | `VideoReceiver.VideoStarted` / `AudioReceiver.AudioStarted` |
| `UseImmersiveSetup` (sender / receiver) | `CubemapVideoSource` / `RenderTextureVideoSink` |
| inspector toggles (`StartStopVideoTransmission`, ...) | Play mode buttons in the inspector |

## Samples
Import the samples from the Samples tab of the package in the Package Manager. The WebSocket samples use `ws://localhost:3000`; start the server from `Server~` or change the URL on the signaling component.
* *WebSocket-TestConnection*: tests a signaling connection without WebRTC.
* *WebRTC-SingleClient-STUNConnection*: one client with buttons for connecting, chat, video and audio. Build it and run several instances.
* *WebRTC-SingleClient-wLobby-STUNConnection*: a simple lobby. `SimpleLobbyManager.cs` shows how to use SimpleWebRTC from C#.
* *WebRTC-MultipleClients-STUNConnection*: three clients in one scene, connected as a full mesh with data, video and audio.
* *WebRTC-Loopback-NoServer*: two peers in one scene exchanging video and chat messages through `LoopbackSignaling`, no server needed. Shows the stats overlay of `WebRTCStatsMonitor`.
* *WebRTC-CustomSignaling-UdpBroadcast*: a custom signaling transport that finds peers in the local network via UDP broadcast, with video, chat and stats. Start it several times or on several devices in the same network, no server needed. On Android/Quest, add the `CHANGE_WIFI_MULTICAST_STATE` permission to your manifest. The sample also contains `ExampleCustomSignaling.cs`, a minimal commented template.
* *WebRTC-SingleClient-STUNConnection-PhotonFusion*: signaling over Photon Fusion 2.
* *WebRTC-SingleClient-STUNConnection-VisualScripting*: SimpleWebRTC with Unity Visual Scripting.
* *WebRTC-SingleClient-STUNConnection-ImmersiveSpectator-Sender/-Receiver*: a 360° video sender and its receiver (e.g. an XR device). The receiver can switch the sender's camera position over the data channel.

## Tutorial videos
These videos show SimpleWebRTC 1.x. The concepts are the same, but the components changed in 2.0.
- [SimpleWebRTC tutorial](https://www.youtube.com/watch?v=-CwJTgt_Z3M)
- [Photon Fusion 2 integration](https://www.youtube.com/watch?v=z1F_cqfdU6o)

## License
This project is licensed under the MIT License.

## Sponsoring
[![Patreon](https://github.com/user-attachments/assets/b5a0f0c0-1227-4dbe-9f2c-10e8c3e7dd41)](https://www.patreon.com/c/WaveLabs)
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/J3J31EWWJB)

## Contributions
Contributions are welcome! Feel free to submit pull requests or report issues.
