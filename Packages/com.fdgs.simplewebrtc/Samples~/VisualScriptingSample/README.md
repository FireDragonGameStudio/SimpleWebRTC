# Visual Scripting Sample

Controls a SimpleWebRTC client from a **Unity Visual Scripting** graph: connect, send messages, start and stop video and audio with the keyboard, and react to WebRTC events, without writing C#.

| Folder | Contents |
|--------|----------|
| `Scene` | `WebRTC-SingleClient-STUNConnection-VisualScripting` |
| `Graph` | `SimpleWebRTC-Graph`, the script graph that drives the client |
| `Input` | `SimpleWebRTC_InputSystem_Actions`, the keyboard bindings |

## Requirements
- SimpleWebRTC 2.x with [WebRTC (FDGS)](https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs) 3.2.0+ and [NativeWebSocket](https://github.com/endel/NativeWebSocket) 2.x
- Unity **Visual Scripting** (`com.unity.visualscripting`) and the **Input System** package
- After importing: **Edit > Project Settings > Visual Scripting > Regenerate Nodes**, so the SimpleWebRTC nodes appear
- The signaling server running on `ws://localhost:3000` (see the *Example Scenes* README)

## How to run
1. Start the signaling server and a second client, e.g. the `WebRTC-SingleClient-STUNConnection` example scene as a build.
2. Open the Visual Scripting scene and press Play.
3. Use the keyboard. Every action and event is logged to the Console with a `SimpleWebRTC VS` prefix:

| Key | Node | Action |
|-----|------|--------|
| **C** | Connect To WebSocket | connect to the signaling server and join the room |
| **W** | Connect To WebRTC | kept for compatibility. In 2.x, peers connect automatically after **C** |
| **D** | Send Data Channel Message | send a test message to all peers over the data channel |
| **T** | Send WebSocket Message | send a raw test text over the signaling channel (other peers log it as an unknown signaling message) |
| **V** / **B** | Start / Stop Video Transmission | start or stop the camera stream |
| **A** / **S** | Start / Stop Audio Transmission | start or stop the audio stream |
| **X** | Disconnect | leave and disconnect |

## How it works
- The client GameObject `Client-STUNConnection` has the normal SimpleWebRTC components (`WebRTCPeerManager`, `WebSocketSignaling`, `DataChannel`, video and audio components).
- It is stored in the scene variable **`ClientConnection`**. The SimpleWebRTC nodes look up the components on that GameObject, so the graph needs no object references.
- `WebRTCConnectionVisualScriptingEvents` on the same GameObject forwards the C# events to the Visual Scripting event nodes.

**Event nodes** (category *Events > SimpleWebRTC*):

| Event node | Fires when |
|------------|------------|
| OnSignalingStateChanged | the signaling connection changes (output: `SignalingState`) |
| OnWebRTCConnected | the WebRTC connection to a peer is established |
| OnDataChannelConnected | a data channel to a peer opened (output: peer id) |
| OnDataChannelMessageReceived | a message arrived (output: message) |
| OnVideoTransmissionReceived | a peer started sending video |
| OnAudioTransmissionReceived | a peer started sending audio |

**Action nodes** (category *SimpleWebRTC*): Connect To WebSocket, Connect To WebRTC, Disconnect, Send Data Channel Message, Send WebSocket Message, Set Unique Player Name, Start/Stop Video Transmission, Start/Stop Audio Transmission.

## Using it in your own scene
1. Set up a client GameObject with the SimpleWebRTC components (or use the `Client-STUNConnection` prefab).
2. Add `WebRTCConnectionVisualScriptingEvents` to it.
3. Add a scene variable `ClientConnection` (type GameObject) pointing to it.
4. Use the SimpleWebRTC nodes in any script graph in that scene.
