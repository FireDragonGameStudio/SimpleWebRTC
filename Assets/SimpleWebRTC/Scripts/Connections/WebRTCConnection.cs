using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WebRTCConnection : MonoBehaviour {

    private const string webSocketTestMessage = "TEST!WEBSOCKET!TEST";
    private const string dataChannelTestMessage = "TEST!CHANNEL!TEST";

    public bool IsWebSocketConnected => webRTCManager.IsWebSocketConnected;
    public bool ConnectionToWebSocketInProgress => webRTCManager.IsWebSocketConnectionInProgress;

    public bool IsWebRTCActive { get; private set; }
    public bool IsVideoTransmissionActive { get; private set; }
    public bool IsAudioChannelActive { get; private set; }

    [Header("Connection Setup")]
    public string WebSocketServerAddress = "wss://unity-webrtc-signaling.glitch.me";
    public string StunServerAddress = "stun:stun.l.google.com:19302";
    public string LocalPeerId = "PeerId";
    public bool UseHTTPHeader = true;
    public bool ShowLogs = true;
    public bool ShowDataChannelLogs = true;
    public bool ShowSpecialLogs = false;

    [Header("WebSocket Connection")]
    public bool WebSocketConnectionActive;
    public bool SendWebSocketTestMessage = false;
    public UnityEvent<WebSocketState> WebSocketConnection;

    [Header("WebRTC Connection")]
    public bool WebRTCConnectionActive = false;
    public UnityEvent WebRTCConnected;

    [Header("Data Transmission")]
    public bool SendDataChannelTestMessage = false;
    public UnityEvent<string> DataChannelMessageReceived;

    [Header("Video Transmission")]
    public bool StartStopVideoTransmission = false;
    public Vector2Int VideoResolution = new Vector2Int(1280, 720);
    public Camera StreamingCamera;
    public RawImage OptionalPreviewRawImage;
    public RectTransform ReceivingRawImagesParent;
    public UnityEvent VideoTransmissionReceived;

    [Header("Audio Transmission")]
    public bool StartStopAudioChannel = false;
    public AudioSource StreamingAudioSource;
    public Transform ReceivingAudioSourceParent;
    public UnityEvent AudioTransmissionReceived;

    private WebRTCManager webRTCManager;

    private void Awake() {
        SimpleWebRTCLogger.EnableLogging = ShowLogs;
        SimpleWebRTCLogger.EnableDataChannelLogging = ShowDataChannelLogs;

        webRTCManager = new WebRTCManager(LocalPeerId, StunServerAddress, this);
    }

    private void Update() {

#if !UNITY_WEBGL || UNITY_EDITOR
        webRTCManager.DispatchMessageQueue();
#endif

        if (SimpleWebRTCLogger.EnableLogging != ShowLogs) {
            SimpleWebRTCLogger.EnableLogging = ShowLogs;
        }

        ConnectToWebSocket();

        if (!WebSocketConnectionActive) {
            DisconnectClient();
        }

        if (!IsWebSocketConnected) {
            return;
        }

        if (SendWebSocketTestMessage) {
            SendWebSocketTestMessage = !SendWebSocketTestMessage;
            webRTCManager.SendWebSocketMessage($"{webSocketTestMessage} from {LocalPeerId}");
        }

        if (WebRTCConnectionActive && !IsWebRTCActive) {
            IsWebRTCActive = !IsWebRTCActive;
            webRTCManager.InstantiateWebRTC();
        }

        if (!WebRTCConnectionActive && IsWebRTCActive) {
            IsWebRTCActive = !IsWebRTCActive;
            webRTCManager.CloseWebRTC();
        }

        if (SendDataChannelTestMessage) {
            SendDataChannelTestMessage = !SendDataChannelTestMessage;
            SendDataChannelMessage($"{dataChannelTestMessage} from {LocalPeerId}");
        }

        if (StartStopVideoTransmission && !IsVideoTransmissionActive) {
            IsVideoTransmissionActive = !IsVideoTransmissionActive;
            StreamingCamera.gameObject.SetActive(IsVideoTransmissionActive);
            webRTCManager.AddVideoTrack(StreamingCamera, VideoResolution.x, VideoResolution.y);
        }

        if (!StartStopVideoTransmission && IsVideoTransmissionActive) {
            IsVideoTransmissionActive = !IsVideoTransmissionActive;
            StreamingCamera.gameObject.SetActive(IsVideoTransmissionActive);
            webRTCManager.RemoveVideoTrack();
        }

        if (StartStopAudioChannel && !IsAudioChannelActive) {
            IsAudioChannelActive = !IsAudioChannelActive;
            StreamingAudioSource.gameObject.SetActive(IsAudioChannelActive);
            StreamingAudioSource.Play();
            webRTCManager.AddAudioTrack(StreamingAudioSource);
        }

        if (!StartStopAudioChannel && IsAudioChannelActive) {
            IsAudioChannelActive = !IsAudioChannelActive;
            StreamingAudioSource.Stop();
            StreamingAudioSource.gameObject.SetActive(IsAudioChannelActive);
            webRTCManager.RemoveAudioTrack();
        }
    }

    private void OnEnable() {
        // only for debugging purposes
        if (OptionalPreviewRawImage != null) {
            webRTCManager.SetOptionalVideoStreamPreviewImage(OptionalPreviewRawImage);
        }

        webRTCManager.OnWebSocketConnection += WebSocketConnection.Invoke;
        webRTCManager.OnWebRTCConnection += WebRTCConnected.Invoke;
        webRTCManager.OnDataChannelMessageReceived += DataChannelMessageReceived.Invoke;
        webRTCManager.OnVideoStreamEstablished += VideoTransmissionReceived.Invoke;
        webRTCManager.OnAudioStreamEstablished += AudioTransmissionReceived.Invoke;

        ConnectToWebSocket();
    }

    private void OnDisable() {
        DisconnectClient();
    }

    private void OnDestroy() {
        DisconnectClient();
    }

    private void ConnectToWebSocket() {
        if (WebSocketConnectionActive && !ConnectionToWebSocketInProgress && !IsWebSocketConnected) {
            webRTCManager.Connect(WebSocketServerAddress, UseHTTPHeader);
        }
    }

    private void DisconnectClient() {
        webRTCManager.CloseWebRTC();
        webRTCManager.CloseWebSocket();

        IsWebRTCActive = false;
        WebRTCConnectionActive = false;

        webRTCManager.OnWebSocketConnection -= WebSocketConnection.Invoke;
        webRTCManager.OnWebRTCConnection -= WebRTCConnected.Invoke;
        webRTCManager.OnDataChannelMessageReceived -= DataChannelMessageReceived.Invoke;
        webRTCManager.OnVideoStreamEstablished -= VideoTransmissionReceived.Invoke;
        webRTCManager.OnAudioStreamEstablished -= AudioTransmissionReceived.Invoke;

        StreamingCamera.gameObject.SetActive(false);
        StreamingAudioSource.Stop();
        StreamingAudioSource.gameObject.SetActive(false);
    }

    public void SendDataChannelMessage(string message) {
        if (!webRTCManager.IsWebSocketConnected) {
            SimpleWebRTCLogger.LogError($"WebSocket not connected on {gameObject.name}");
            return;
        }
        webRTCManager.SendViaDataChannel(message);
    }
}