# Immersive Video Sample (360°)

Streams a **360° video** of a 3D scene from a *sender* to a *receiver*, typically from a PC to an XR headset or a spectator screen. The receiver can look around freely in the panorama and switch the sender's camera position over a data channel.

| Scene | Role |
|-------|------|
| `WebRTC-SingleClient-STUNConnection-ImmersiveSpectator-Sender` | Renders the scene with a 360° camera and streams it, plus audio |
| `WebRTC-SingleClient-STUNConnection-ImmersiveSpectator-Receiver` | Shows the stream as a 360° skybox and sends camera switch commands |

360° rendering is expensive: the sender renders a cubemap, up to six camera renders per frame, and converts it to an equirectangular image before encoding it.

## Requirements
- SimpleWebRTC 2.x with [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) 3.2.0+ and [NativeWebSocket](https://github.com/endel/NativeWebSocket) 2.x
- The signaling server running on `ws://localhost:3000` (see the *Example Scenes* README), or your own URL on the `WebSocketSignaling` components
- The sample uses materials and RenderTextures from the package's `Runtime` folder

## How to run
1. Start the signaling server.
2. Start the **sender**, e.g. as a build. Its `SpectatorLogic` connects automatically on start (*Connect On Start* on `SwitchToNextCameraPosition`).
3. Start the **receiver** in the Editor or on the headset. It connects automatically as well.
4. In the sender, press **Start Video** (and **Start Audio**). The receiver's sky turns into the live 360° view of the sender's scene. To look around, rotate the receiver's `Main Camera` (in the Editor via the Inspector or Scene view), or replace it with your XR camera rig on a headset.
5. In the receiver, press **Send Camera Switch**. The sender moves its 360° camera to the next of its five positions.
6. **Toggle UI** hides the button panel in both scenes.

## Sender
`Client-STUNConnection-ImmersiveSender`:

| Component | Setting |
|-----------|---------|
| `WebSocketSignaling` + `WebRTCPeerManager` | server `ws://localhost:3000`, peer id `Red` + random suffix |
| `CubemapVideoSource` | camera `Client-StreamingCamera`, mono, **2048 px** per cube face, *One Face Per Frame* on (renders one of six faces per frame to save GPU time) |
| `VideoSender` | streams the equirect texture |
| `AudioSender` | streams the ambience AudioSource |
| `DataChannel` | receives the camera switch commands |

`SpectatorLogic` (`SwitchToNextCameraPosition`) holds the five camera positions (**Camera Parent Objects**). When the data channel receives `switch`, it re-parents the streaming camera to the next position.

## Receiver
`Client-STUNConnection-ImmersiveReceiver`:

| Component | Setting |
|-----------|---------|
| `WebSocketSignaling` + `WebRTCPeerManager` | same server and room |
| `VideoReceiver` + `RenderTextureVideoSink` | copies the received video into `Runtime/RenderTexture/VideoReceiver_RT` every frame |
| `AudioReceiver` | plays the sender's audio |
| `DataChannel` | sends `switch` (button **Send Camera Switch**) |

The scene's skybox is `Runtime/Materials/VideoReceiver_RT_360`, a *Skybox/Panoramic* material that uses `VideoReceiver_RT`. The received equirect video becomes the sky around the camera. Nothing else in the scene has to know about WebRTC.

## Options
- **Stereo (3D) 360°:** enable **Render Stereo** on the sender's `CubemapVideoSource`. Both eyes are stacked over-under, so the video is twice as high. On the receiver, use `VideoReceiver_Stereo_RT` as **Target Texture** of the `RenderTextureVideoSink`, and set the skybox material's *3D Layout* to *Over Under*.
- **Quality vs. performance:** lower **Eye Resolution** (e.g. 1024) or keep *One Face Per Frame* on for weaker GPUs. Set **Max Bitrate Kbps** on the `VideoSender` to limit bandwidth. Add a `WebRTCStatsMonitor` to see the effect.
- **Experimental 6DOF:** on the receiver's `SpectatorLogic`, enable **Sync Camera Position**. The receiver sends the local position of its **Spectator Camera** (the Main Camera / headset) ten times per second, and the sender moves its 360° camera accordingly. This only moves the camera and is not true 6DOF video.
- **Your own scene:** add a `CubemapVideoSource` + `VideoSender` to any client and point it to a camera in your scene. On the receiving side, a `RenderTextureVideoSink` and a panoramic skybox are all you need.
