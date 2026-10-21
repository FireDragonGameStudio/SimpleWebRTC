# Photon Fusion 2 Sample

Uses a running **Photon Fusion 2** session as the signaling channel instead of a WebSocket server. If your game already uses Fusion for multiplayer, the players can find each other there and send video, audio and data peer-to-peer over WebRTC.

## Requirements
- SimpleWebRTC 2.x with [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) 3.2.0+
- [Photon Fusion 2](https://assetstore.unity.com/packages/tools/network/photon-fusion-267958) installed and set up with your Photon App Id. This defines `FUSION2`, which compiles SimpleWebRTC's Photon assembly.
- In **NetworkProjectConfig > Assemblies To Weave**, add `SimpleWebRTC.PhotonFusion`, so Fusion processes the RPCs of the signaling relay.

## Setup after importing
This scene was created with SimpleWebRTC 1.x and still contains the old `WebRTCConnection_Generic` component. Convert it once:
1. Install Photon Fusion 2 (see above) and let Unity recompile.
2. Open `WebRTC-SingleClient-STUNConnection-PhotonFusion`.
3. Run **Tools > SimpleWebRTC > Migrate Open Scenes** and save the scene.

The migration replaces the old component with `WebRTCPeerManager`, `PhotonFusionSignaling`, `DataChannel`, `VideoSender`/`VideoReceiver` and `AudioSender`/`AudioReceiver`, and keeps the buttons working.

## How to run
1. Start two instances, e.g. a build and the Editor.
2. In both, start the Fusion session with the Fusion prototype UI (`Prototype Network Start`), e.g. *Start Shared Client* with the same session name.
3. Press **Connect**. As soon as the `PhotonSignalingRelay` on `Photon SignalServer` is spawned in the session, signaling switches to `Connected` and the peers connect.
4. Use **Start/Stop Video**, **Start/Stop Audio** and **DataChannel Message** like in the other samples.
5. **Disconnect** leaves the WebRTC connections. **Disconnect Photon** shuts down the Fusion session (`PhotonSignalingRelay.DisconnectFromServer()`).

## How it works
- `PhotonSignalingRelay` is a `NetworkBehaviour` on a scene NetworkObject. It sends every signaling message as an RPC to all other players. Fusion RPCs are limited in size, so messages are split into chunks of 400 bytes and reassembled on the other side.
- `PhotonFusionSignaling` is the `SignalingTransport` for the `WebRTCPeerManager`. It is **Connected** while its relay is spawned. It does not start or stop Fusion itself; your game controls the session.
- Only signaling goes through Photon. Video, audio and data channels go directly between the players via WebRTC, so they don't count against your Photon traffic.

To use it in your own project: put a `PhotonSignalingRelay` on a NetworkObject that exists for all players, add a `PhotonFusionSignaling` next to your `WebRTCPeerManager`, reference them from each other, and call `Connect()` once the Fusion session is running.
