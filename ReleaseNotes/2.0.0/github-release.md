# SimpleWebRTC 2.0.0

A complete rework of SimpleWebRTC. The all-in-one `WebRTCConnection` is now split into small components that you combine as needed, the signaling backend is swappable, and connections are much more robust. 2.0 builds on [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) 3.2.0, the community-maintained fork of Unity's WebRTC package.

> ⚠️ **This is a breaking release.** Scenes, scripts and the signaling protocol changed. Read [Upgrading from 1.x](#upgrading-from-1x) before updating.

## Highlights
- **Component-based:** `WebRTCPeerManager` plus a signaling transport, `DataChannel`, `VideoSender`/`VideoReceiver` with exchangeable sources and sinks, `AudioSender`/`AudioReceiver` and `WebRTCStatsMonitor`. Add only what you need.
- **Pluggable signaling:** WebSocket (NativeWebSocket 2.x or Meta Voice SDK), Photon Fusion 2, in-process loopback, a LAN/UDP broadcast example, or your own backend: derive from `SignalingTransport` and implement three methods.
- **Robust connections:** "perfect negotiation" lets any peer start or stop streams at any time. Peers that join later receive streams that are already running, and reconnecting works reliably.
- **Connection statistics:** round trip time, bitrates, FPS, resolution, codec and encoder, packet loss, jitter, and whether the connection is direct or relayed through TURN, with an optional on-screen overlay.
- **No server needed to get started:** the new Loopback sample connects two peers in one scene, and the UDP sample connects devices in the same network.
- **Migration tool** for 1.x scenes, including settings, UnityEvent listeners and button calls.
- **Browser client:** the updated [SimpleWebRTC-Web](https://github.com/FireDragonGameStudio/SimpleWebRTC-Web) lets web browsers join the same rooms as Unity apps.

## Added
- Component architecture: `WebRTCPeerManager`, `SignalingTransport` base class, `DataChannel`, `VideoSender`, `VideoReceiver`, `AudioSender`, `AudioReceiver`, `WebRTCStatsMonitor`.
- Signaling transports: `WebSocketSignaling` (NativeWebSocket 2.x), `MetaWebSocketSignaling` (Meta Voice SDK), `PhotonFusionSignaling` + `PhotonSignalingRelay`, `LoopbackSignaling`.
- Rooms, so several apps can share one signaling server.
- Video sources: camera, any texture, webcam, 360° cubemap (mono or stereo over-under). Video sinks: RawImage (fixed or one per peer), renderer, RenderTexture.
- Multiple data channels per connection: reliable or unreliable, text or binary, with per-peer events and `SendTo(peerId, ...)`.
- Microphone support for `AudioSender`, and an optional AudioSource prefab for received audio (spatial audio, mixer groups).
- STUN **and TURN** server list, preferred video codec (VP8 by default, others as fallback), bitrate and framerate limits.
- `WebRTCStatsMonitor` with events, a formatted text event for UI and an optional IMGUI overlay.
- Play mode buttons and live status in the inspector (Connect, Start Streaming, Send Test Message, ...).
- Tools > SimpleWebRTC > Migrate Open Scenes, which converts 1.x components.
- Dependency check that tells you what's missing and installs WebRTC (FDGS) or NativeWebSocket with one click.
- Ready-to-run Node.js signaling server in `Server~`.
- Samples: **Loopback** (no server) and **Custom Signaling Transport** (UDP broadcast in the LAN). All samples now have their own README.
- Documentation: a "How WebRTC works" guide with diagrams, plus EditMode and PlayMode tests.

## Changed
- Requires **Unity 6000.0** or newer and **WebRTC (FDGS) 3.2.0+** (`com.fdgs.webrtc`) instead of `com.unity.webrtc`.
- WebSocket signaling requires **NativeWebSocket 2.x** (`https://github.com/endel/NativeWebSocket.git#upm-2`), or the Meta Voice SDK.
- New JSON-based signaling protocol (v2) with rooms. **Not compatible with 1.x clients** or the 1.x web client. Use the current [SimpleWebRTC-Web](https://github.com/FireDragonGameStudio/SimpleWebRTC-Web) as browser client.
- The package is renamed from `com.firedragongamestudio.simplewebrtc` to **`com.fdgs.simplewebrtc`**. In this repository it now lives in `Packages/com.fdgs.simplewebrtc`.
- WebSocket, Meta WebSocket, Photon Fusion and Visual Scripting support are separate assemblies, which compile only when their dependency is installed. The package no longer fails to compile because an optional SDK is missing.
- Sent video uses VP8 by default through codec preferences, with other codecs as fallback, instead of rewriting the SDP.

## Fixed
- After a disconnect and reconnect, no signaling messages were sent anymore.
- Peers that joined after video or audio had started never received those streams.
- The `WebRTC.Update` coroutine was started once per connection and could not be stopped; the same applied to the media start coroutines.
- Malformed signaling messages crashed message parsing.
- Errors were not logged when logging was disabled.
- Received video stayed frozen on the last frame after the sender stopped.
- Connections between peers with different media setups (e.g. sender with video and audio, receiver with data only) could get stuck during the first negotiation.

## Upgrading from 1.x
1. Remove the 1.x package (`com.firedragongamestudio.simplewebrtc`), or delete `Assets/SimpleWebRTC` if you imported it as an asset. 2.x uses the same script GUIDs, so the two must not be installed side by side.
2. Install the dependencies:
   - `https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs.git#v3.2.0`
   - `https://github.com/endel/NativeWebSocket.git#upm-2` (skip this if you use the Meta Voice SDK)
3. Install SimpleWebRTC 2.0:
   - `https://github.com/FireDragonGameStudio/SimpleWebRTC.git?path=/Packages/com.fdgs.simplewebrtc`
4. Open each scene that uses `WebRTCConnection` or `WebRTCConnection_Generic` and run **Tools > SimpleWebRTC > Migrate Open Scenes**.
5. Update your scripts. The README has a [table mapping 1.x members to 2.x](https://github.com/FireDragonGameStudio/SimpleWebRTC#migrating-from-simplewebrtc-1x).
6. Update every client at the same time: 1.x and 2.x clients can't connect to each other. If you used your own signaling server, the relay server from `Server~` (or any server that forwards messages to all other clients) works with 2.x.

## Known issues
- The Photon Fusion 2 sample scene still contains the 1.x component. After installing Photon Fusion 2 and importing the sample, run **Tools > SimpleWebRTC > Migrate Open Scenes** once.
- SimpleWebRTC connects every peer to every other peer (full mesh). For video this works well up to about 4 to 6 peers; larger groups need a media server (SFU).
- WebGL is not supported (WebRTC (FDGS) has no WebGL backend).
