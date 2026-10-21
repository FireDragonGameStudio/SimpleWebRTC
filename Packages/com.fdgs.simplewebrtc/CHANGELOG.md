# Changelog
All notable changes to SimpleWebRTC are documented in this file.

## [2.0.0] - 2026-10-20

### Breaking changes
- `WebRTCConnection`, `WebRTCManager` and their `_Generic` variants are replaced by components: `WebRTCPeerManager`, signaling transports, `DataChannel`, `VideoSender`/`VideoReceiver` with sources and sinks, and `AudioSender`/`AudioReceiver`. Convert existing scenes with Tools > SimpleWebRTC > Migrate Open Scenes.
- New JSON signaling protocol (v2) with rooms. It is not compatible with 1.x clients or the 1.x web client.
- Requires Unity 6000.0 and WebRTC (FDGS) `com.fdgs.webrtc` 3.2.0 instead of `com.unity.webrtc`. Requires NativeWebSocket 2.x for `WebSocketSignaling`.
- WebSocket, Meta WebSocket, Photon Fusion and Visual Scripting support live in separate assemblies, which are only compiled when their dependency is installed.
- The package was renamed from `com.firedragongamestudio.simplewebrtc` to `com.fdgs.simplewebrtc`, matching `com.fdgs.webrtc`, and moved to `Packages/com.fdgs.simplewebrtc` in the repository. Remove the 1.x package before adding 2.x. The git install URL is now `https://github.com/FireDragonGameStudio/SimpleWebRTC.git?path=/Packages/com.fdgs.simplewebrtc`.

### Added
- Perfect negotiation: tracks and data channels can be added or changed on any side at any time.
- `LoopbackSignaling` for peers in one app, and a Loopback sample that needs no server.
- `SignalingTransport` base class for custom signaling backends, plus a Custom Signaling Transport sample with a working UDP broadcast transport (LAN, no server) and a minimal template.
- Rooms, so several apps can share one signaling server.
- Multiple data channels per connection (reliable or unreliable, text or binary) and per-peer message events.
- Video sources: camera, texture, webcam, 360° cubemap. Video sinks: RawImage (fixed or spawned per peer), renderer, RenderTexture.
- Microphone support for `AudioSender`; optional AudioSource prefab for received audio.
- STUN and TURN server list, preferred video codec (VP8 by default, other codecs as fallback), bitrate and framerate limits.
- `WebRTCStatsMonitor`: per-peer round trip time, bitrates, frame rate, resolution, codec and encoder, packet loss, jitter and network path (direct or TURN), with events and an optional on-screen overlay.
- Play mode inspector buttons and runtime state info.
- Migration tool for 1.x scenes, including UnityEvent listeners and button calls.
- Dependency check with one-click install of WebRTC (FDGS) and NativeWebSocket.
- Node.js signaling server in `Server~`.
- EditMode and PlayMode tests.

### Fixed
- Signaling messages were no longer sent after a disconnect and reconnect.
- Peers joining after video or audio had started never received those streams.
- `WebRTC.Update` was started once per connection and could not be stopped, and the media start coroutines could not be stopped either.
- Malformed signaling messages crashed message parsing.
- Errors were not logged when logging was disabled.
- Received video stayed frozen on the last frame after the sender stopped.
- Connections between peers with different media setups (e.g. sender with video and audio, receiver with data only) could get stuck during the first negotiation.
