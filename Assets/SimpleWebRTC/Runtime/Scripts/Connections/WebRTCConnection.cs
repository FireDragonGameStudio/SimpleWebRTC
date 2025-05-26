using NativeWebSocket;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class WebRTCConnection : MonoBehaviour {

        private const string webSocketTestMessage = "TEST!WEBSOCKET!TEST";
        private const string dataChannelTestMessage = "TEST!CHANNEL!TEST";

        public bool IsWebSocketConnected => webRTCManager.IsWebSocketConnected;
        public bool ConnectionToWebSocketInProgress => webRTCManager.IsWebSocketConnectionInProgress;

        public bool IsWebRTCActive { get; private set; }
        public bool IsVideoTransmissionActive { get; private set; }
        public bool IsAudioTransmissionActive { get; private set; }

        [Header("Connection Setup")]
        [SerializeField] private string WebSocketServerAddress = "wss://unity-webrtc-signaling.glitch.me";
        [SerializeField] private string StunServerAddress = "stun:stun.l.google.com:19302";
        [SerializeField] private string LocalPeerId = "PeerId";
        [SerializeField] private bool UseHTTPHeader = true;
        [SerializeField] private bool IsVideoAudioSender = true;
        [SerializeField] private bool IsVideoAudioReceiver = true;
        [SerializeField] private bool RandomUniquePeerId = true;
        [SerializeField] private bool ShowLogs = true;
        [SerializeField] private bool ShowDataChannelLogs = true;

        [Header("WebSocket Connection")]
        [SerializeField] private bool WebSocketConnectionActive;
        [SerializeField] private bool SendWebSocketTestMessage = false;
        public UnityEvent<WebSocketState> WebSocketConnectionChanged;

        [Header("WebRTC Connection")]
        [SerializeField] private bool WebRTCConnectionActive = false;
        public UnityEvent WebRTCConnected;

        [Header("Data Transmission")]
        [SerializeField] private bool SendDataChannelTestMessage = false;
        public UnityEvent<string> DataChannelConnected;
        public UnityEvent<string> DataChannelMessageReceived;

        [Header("Video Transmission")]
        [SerializeField] private bool StartStopVideoTransmission = false;
        [SerializeField] private Vector2Int VideoResolution = new Vector2Int(1280, 720);
        [SerializeField] private Camera StreamingCamera;
        public RawImage OptionalPreviewRawImage;
        public RectTransform ReceivingRawImagesParent;
        public UnityEvent VideoTransmissionReceived;

        [Header("Audio Transmission")]
        [SerializeField] private bool StartStopAudioTransmission = false;
        [SerializeField] private AudioSource StreamingAudioSource;
        public Transform ReceivingAudioSourceParent;
        public UnityEvent AudioTransmissionReceived;

        private WebRTCManager webRTCManager;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;

        private void Awake() {
            SimpleWebRTCLogger.EnableLogging = ShowLogs;
            SimpleWebRTCLogger.EnableDataChannelLogging = ShowDataChannelLogs;

            if (RandomUniquePeerId) {
                LocalPeerId = GenerateRandomUniquePeerId();
            }
            webRTCManager = new WebRTCManager(LocalPeerId, StunServerAddress, this);

            // register events for webrtc connection
            webRTCManager.OnWebSocketConnection += WebSocketConnectionChanged.Invoke;
            webRTCManager.OnWebRTCConnection += WebRTCConnected.Invoke;
            webRTCManager.OnDataChannelConnection += DataChannelConnected.Invoke;
            webRTCManager.OnDataChannelMessageReceived += DataChannelMessageReceived.Invoke;
            webRTCManager.OnVideoStreamEstablished += VideoTransmissionReceived.Invoke;
            webRTCManager.OnAudioStreamEstablished += AudioTransmissionReceived.Invoke;
        }

        private void Update() {

#if !UNITY_WEBGL || UNITY_EDITOR
            webRTCManager.DispatchMessageQueue();
#endif

            if (SimpleWebRTCLogger.EnableLogging != ShowLogs) {
                SimpleWebRTCLogger.EnableLogging = ShowLogs;
            }

            ConnectClient();

            if (!WebSocketConnectionActive && IsWebSocketConnected) {
                DisconnectClient();
            }

            if (!IsWebSocketConnected) {
                return;
            }

            if (SendWebSocketTestMessage) {
                SendWebSocketTestMessage = !SendWebSocketTestMessage;
                webRTCManager.SendWebSocketTestMessage($"{webSocketTestMessage} from {LocalPeerId}");
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

            if (StartStopVideoTransmission && !IsVideoTransmissionActive && IsVideoAudioSender) {
                IsVideoTransmissionActive = !IsVideoTransmissionActive;
                StartVideoTransmission();
            }

            if (!StartStopVideoTransmission && IsVideoTransmissionActive) {
                IsVideoTransmissionActive = !IsVideoTransmissionActive;
                StopVideoTransmission();
            }

            if (StartStopAudioTransmission && !IsAudioTransmissionActive && IsVideoAudioSender) {
                IsAudioTransmissionActive = !IsAudioTransmissionActive;
                StartAudioTransmission();
            }

            if (!StartStopAudioTransmission && IsAudioTransmissionActive) {
                IsAudioTransmissionActive = !IsAudioTransmissionActive;
                StopAudioTransmission();
            }
        }

        private void OnEnable() {
            ConnectClient();
        }

        private void OnDisable() {
            DisconnectClient();
        }

        private void OnDestroy() {
            DisconnectClient();

            // de-register events for connection
            webRTCManager.OnWebSocketConnection -= WebSocketConnectionChanged.Invoke;
            webRTCManager.OnWebRTCConnection -= WebRTCConnected.Invoke;
            webRTCManager.OnDataChannelConnection -= DataChannelConnected.Invoke;
            webRTCManager.OnDataChannelMessageReceived -= DataChannelMessageReceived.Invoke;
            webRTCManager.OnVideoStreamEstablished -= VideoTransmissionReceived.Invoke;
            webRTCManager.OnAudioStreamEstablished -= AudioTransmissionReceived.Invoke;
        }

        private string GenerateRandomUniquePeerId() {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            int length = Random.Range(3, 6); // Generates a length between 3 and 5
            char[] nameChars = new char[length];

            for (int i = 0; i < length; i++) {
                nameChars[i] = chars[Random.Range(0, chars.Length)];
            }

            return new string(nameChars) + "-PeerId";
        }

        private void ConnectClient() {
            if (WebSocketConnectionActive && !ConnectionToWebSocketInProgress && !IsWebSocketConnected) {
                webRTCManager.Connect(WebSocketServerAddress, UseHTTPHeader, IsVideoAudioSender, IsVideoAudioReceiver);
            }
        }

        private void DisconnectClient() {
            // stop websocket
            WebSocketConnectionActive = false;

            // stop webRTC
            IsWebRTCActive = false;
            WebRTCConnectionActive = false;

            // stop video
            StartStopVideoTransmission = false;
            IsVideoTransmissionActive = false;
            if (OptionalPreviewRawImage != null) {
                OptionalPreviewRawImage.texture = null;
            }
            if (StreamingCamera != null) {
                StreamingCamera.gameObject.SetActive(IsVideoTransmissionActive);
            }
            webRTCManager.RemoveVideoTrack();

            // stop audio
            StartStopAudioTransmission = false;
            IsAudioTransmissionActive = false;
            if (StreamingAudioSource != null) {
                StreamingAudioSource.Stop();
                StreamingAudioSource.gameObject.SetActive(IsAudioTransmissionActive);
            }
            webRTCManager.RemoveAudioTrack();

            webRTCManager.CloseWebRTC();
            webRTCManager.CloseWebSocket();

            if (StreamingCamera != null) {
                StreamingCamera.gameObject.SetActive(false);
            }
            if (StreamingAudioSource != null) {
                StreamingAudioSource.Stop();
                StreamingAudioSource.gameObject.SetActive(false);
            }
        }

        public void SetUniquePlayerName(string playerName) {
            LocalPeerId = playerName;
        }

        public void Connect() {
            WebSocketConnectionActive = true;
        }

        public void ConnectWebRTC() {
            WebRTCConnectionActive = true;
        }

        public void Disconnect() {
            WebSocketConnectionActive = false;
        }

        public void SendDataChannelMessage(string message) {
            if (!webRTCManager.IsWebSocketConnected) {
                SimpleWebRTCLogger.LogError($"WebSocket not connected on {gameObject.name}");
                return;
            }
            webRTCManager.SendViaDataChannel(message);
        }

        public void SendDataChannelMessageToPeer(string targetPeerId, string message) {
            if (!webRTCManager.IsWebSocketConnected) {
                SimpleWebRTCLogger.LogError($"WebSocket not connected on {gameObject.name}");
                return;
            }
            webRTCManager.SendViaDataChannel(targetPeerId, message);
        }

        public void StartVideoTransmission() {
            StopCoroutine(StartVideoTransmissionAsync());
            StartCoroutine(StartVideoTransmissionAsync());
        }

        private IEnumerator StartVideoTransmissionAsync() {

            StreamingCamera.gameObject.SetActive(true);

            // camera activation delay?
            yield return new WaitForSeconds(1f);

            if (IsVideoTransmissionActive) {
                // for restarting without stopping
                webRTCManager.RemoveVideoTrack();
            }
            videoStreamTrack = StreamingCamera.CaptureStreamTrack(VideoResolution.x, VideoResolution.y);
            webRTCManager.AddVideoTrack(videoStreamTrack);

            StartStopVideoTransmission = true;
            IsVideoTransmissionActive = true;
        }

        public void StopVideoTransmission() {

            StopCoroutine(StartVideoTransmissionAsync());

            StreamingCamera.gameObject.SetActive(false);

            videoStreamTrack?.Stop();
            webRTCManager.RemoveVideoTrack();

            videoStreamTrack?.Dispose();
            videoStreamTrack = null;

            StartStopVideoTransmission = false;
            IsVideoTransmissionActive = false;
        }

        public void StartAudioTransmission() {
            StopCoroutine(StartAudioTransmissionAsync());
            StartCoroutine(StartAudioTransmissionAsync());
        }

        private IEnumerator StartAudioTransmissionAsync() {

            StopCoroutine(StartAudioTransmissionAsync());

            StreamingAudioSource.gameObject.SetActive(true);

            // audio activation delay?
            yield return new WaitForSeconds(1f);

            StreamingAudioSource.Play();

            if (IsAudioTransmissionActive) {
                // for restarting without stopping
                webRTCManager.RemoveAudioTrack();
            }
            audioStreamTrack = new AudioStreamTrack(StreamingAudioSource) {
                Loopback = true
            };
            webRTCManager.AddAudioTrack(audioStreamTrack);

            StartStopAudioTransmission = true;
            IsAudioTransmissionActive = true;
        }

        public void StopAudioTransmission() {

            StreamingAudioSource.Stop();
            StreamingAudioSource.gameObject.SetActive(IsAudioTransmissionActive);

            audioStreamTrack?.Stop();
            webRTCManager.RemoveAudioTrack();

            audioStreamTrack?.Dispose();
            audioStreamTrack = null;

            StartStopAudioTransmission = false;
            IsAudioTransmissionActive = false;
        }
    }
}