![simple-webrtc-logo](https://github.com/user-attachments/assets/4a243ff7-8260-4277-8d7a-844f67ffdcff)

# SimpleWebRTC
SimpleWebRTC is a Unity-based WebRTC wrapper that facilitates peer-to-peer audio, video, and data communication over WebRTC using Unitys WebRTC package [https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/index.html](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/index.html). It leverages NativeWebSocket [https://github.com/endel/NativeWebSocket](https://github.com/endel/NativeWebSocket) for signaling and supports both video and audio streaming.

## Features
- WebRTC peer-to-peer connection management
- WebSocket-based signaling
- Video and audio streaming
- Data channel communication
- Logging and debugging tools

## Tutorial video
A tutorial YouTube video can be found here: [https://www.youtube.com/watch?v=-CwJTgt_Z3M](https://www.youtube.com/watch?v=-CwJTgt_Z3M)

## Simple Installation
1. Make sure, that the required dependencies are installed (`TextMeshPro`, `Unity WebRTC`, `NativeWebSocket`).
2. Go to the Unity AssetStore page: [https://assetstore.unity.com/packages/slug/309727](https://assetstore.unity.com/packages/slug/309727)
4. Install the package via Unity AssetStore.

## Installation using the releases page
1. Got to the releases page and download the latest release.
2. Make sure, that the required dependencies are installed (`TextMeshPro`, `Unity WebRTC`, `NativeWebSocket`).
3. Import the package into your Unity project.

## Installation using Unity Package Manager
1. Create a new Unity project
2. Open the Package Manager, click on the + sign in the upper left/right corner
3. Select "Add package from git URL"
4. Insert this link `https://github.com/FireDragonGameStudio/SimpleWebRTC.git?path=/Assets/SimpleWebRTC#upm` and click on Install

## Manual Installation
1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/simplewebrtc.git
   ```
2. Open the Unity project in the Unity Editor.
3. Ensure that the required dependencies (such as `TextMeshPro`, `Unity WebRTC` and `NativeWebSocket`) are installed.

## Usage
### WebRTCConnection Component
The `WebRTCConnection` component manages the WebRTC connection and can be attached to a GameObject in Unity.

![image](https://github.com/user-attachments/assets/e13c49e9-c9dc-4a5c-9200-94efd4800b1c)

### Public Properties
| Property | Type | Description |
|----------|------|-------------|
| `IsWebSocketConnected` | `bool` | Indicates whether the WebSocket connection is active. |
| `ConnectionToWebSocketInProgress` | `bool` | Indicates whether a connection attempt is in progress. |
| `IsWebRTCActive` | `bool` | Shows if a WebRTC session is active. |
| `IsVideoTransmissionActive` | `bool` | Indicates whether video transmission is active. |
| `IsAudioTransmissionActive` | `bool` | Indicates whether audio transmission is active. |

### Public Methods
| Method | Description |
|--------|-------------|
| `void Connect()` | Initiates a WebSocket connection and establishes WebRTC connections as soon as other peers are connected. |
| `void Disconnect()` | Closes the WebSocket connection and disconnects the WebRTC connections with other peers. |
| `void SendDataChannelMessage(string message)` | Sends a message via the data channel. |
| `void SendDataChannelMessageToPeer(string targetPeerId, string message)` | Sends a data channel message to a specific peer. |
| `void StartVideoTransmission()` | Starts video transmission. |
| `void StopVideoTransmission()` | Stops video transmission. |
| `void StartAudioTransmission()` | Starts audio transmission. |
| `void StopAudioTransmission()` | Stops audio transmission. |
| `void SetUniquePlayerName(string playerName)` | Sets a unique identifier for the peer. |

### Events
| Event | Description |
|-------|-------------|
| `WebSocketConnectionChanged` | Triggered when the WebSocket connection state changes. |
| `WebRTCConnected` | Invoked when a WebRTC connection is successfully established. |
| `DataChannelConnected` | Raised when the data channel connection is established. |
| `DataChannelMessageReceived` | Fired when a message is received via the data channel. |
| `VideoTransmissionReceived` | Triggered when a video stream is received. |
| `AudioTransmissionReceived` | Triggered when an audio stream is received. |

### Configuration
The `WebRTCConnection` component includes several configurable parameters:
```csharp
[SerializeField] private string WebSocketServerAddress = "wss://unity-webrtc-signaling.glitch.me";
[SerializeField] private string StunServerAddress = "stun:stun.l.google.com:19302";
[SerializeField] private string LocalPeerId = "PeerId"; // must be unique for each peer
[SerializeField] private bool UseHTTPHeader = true; // used for e.g. Glitch.com, because headers are needed
[SerializeField] private bool ShowLogs = true; // mostly for debugging purposes, can be disabled
[SerializeField] private bool ShowDataChannelLogs = true; // mostly for debugging purposes, can be disabled
```

Modify these values in the Unity Inspector or directly in the script.

## Example
Following sample scenes are included in the pacakge:
* *WebSocket-TestConnection*: For testing the wecksocket connection separately.
* *WebRTC-SingleClient-STUNConnection*: Testing STUN connection for a single client. Works standalone and can be deployed to clients. Make sure to set the `LocalPeerId` for each client individually.
* *WebRTC-SingleClient-wLobby-STUNConnection*: A simple Lobby example for handling multiple STUN WebRTC clients. `SimpleLobbyManager.cs` shows an example, how to use **SimpleWebRTC** via C#.
* *WebRTC-MultipleClients-STUNConnection*: Shows how multiple clients can be connected via peer-to-peer connections and share data, video and audio transmissions.

## Example code
```csharp
WebRTCConnection connection = gameObject.GetComponent<WebRTCConnection>();
connection.Connect(); // Establish WebSocket connection

// after a WebRTC peer-to-peer connection is established
connection.StartVideoTransmission(); // Begin video streaming
connection.SendDataChannelMessage("Hello Peer!"); // Send a message over the data channel
```

## License
This project is licensed under the MIT License.

## Contributions
Contributions are welcome! Feel free to submit pull requests or report issues.

