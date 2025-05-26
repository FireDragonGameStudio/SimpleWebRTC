using System.Collections;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class WebRTCConnection_Generic : MonoBehaviour {

        private const string signalServerTestMessage = "TEST!SIGNALSERVER!TEST";
        private const string dataChannelTestMessage = "TEST!CHANNEL!TEST";

        private const int maxChunkSize = 400; // Max possible size is usually 512 byte, but we'll allow some headroom for message metadata

        public string PeerId => LocalPeerId;

        public bool IsWebRTCActive { get; private set; }
        public bool IsVideoTransmissionActive { get; private set; }
        public bool IsAudioTransmissionActive { get; private set; }

        [Header("Connection Setup")]
        [SerializeField] private string StunServerAddress = "stun:stun.l.google.com:19302";
        [SerializeField] private string LocalPeerId = "PeerId";
        [SerializeField] private bool IsVideoAudioSender = true;
        [SerializeField] private bool IsVideoAudioReceiver = true;
        [SerializeField] private bool RandomUniquePeerId = true;
        [SerializeField] private bool ShowLogs = true;
        [SerializeField] private bool ShowDataChannelLogs = true;

        [Header("SignalServer Connection")]
        [SerializeField] private bool SendSignalServerTestMessage = false;
        [SerializeField] private PhotonSignalServer SignalServerMessageHandler;

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

        private WebRTCManager_Generic webRTCManager;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;

        private void Awake() {
            SimpleWebRTCLogger.EnableLogging = ShowLogs;
            SimpleWebRTCLogger.EnableDataChannelLogging = ShowDataChannelLogs;

            if (RandomUniquePeerId) {
                LocalPeerId = GenerateRandomUniquePeerId();
            }
            webRTCManager = new WebRTCManager_Generic(LocalPeerId, StunServerAddress, this, IsVideoAudioSender, IsVideoAudioReceiver);

            // register events for webrtc connection
            webRTCManager.OnWebRTCConnection += WebRTCConnected.Invoke;
            webRTCManager.OnDataChannelConnection += DataChannelConnected.Invoke;
            webRTCManager.OnDataChannelMessageReceived += DataChannelMessageReceived.Invoke;
            webRTCManager.OnVideoStreamEstablished += VideoTransmissionReceived.Invoke;
            webRTCManager.OnAudioStreamEstablished += AudioTransmissionReceived.Invoke;

            if (SignalServerMessageHandler == null) {
                SimpleWebRTCLogger.LogError("No SignalServerMessageHandler set! WebRTC connection will not work!");
            }
        }

        private void Update() {

            if (SimpleWebRTCLogger.EnableLogging != ShowLogs) {
                SimpleWebRTCLogger.EnableLogging = ShowLogs;
            }

            if (SendSignalServerTestMessage) {
                SendSignalServerTestMessage = !SendSignalServerTestMessage;
                webRTCManager.SendSignalServerTestMessage($"{signalServerTestMessage} from {LocalPeerId}");
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

        private void OnDestroy() {
            Disconnect();

            // de-register events for connection
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

        public void Disconnect() {

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

            if (StreamingCamera != null) {
                StreamingCamera.gameObject.SetActive(false);
            }
            if (StreamingAudioSource != null) {
                StreamingAudioSource.Stop();
                StreamingAudioSource.gameObject.SetActive(false);
            }

            webRTCManager.Disconnect();
        }

        public void SetUniquePlayerName(string playerName) {
            LocalPeerId = playerName;
        }

        public void Connect() {
            webRTCManager.Connect();
        }

        public void ConnectWebRTC() {
            WebRTCConnectionActive = true;
        }

        public void SendDataChannelMessage(string message) {
            webRTCManager.SendViaDataChannel(message);
        }

        public void SendDataChannelMessageToPeer(string targetPeerId, string message) {
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
            StreamingAudioSource.gameObject.SetActive(false);

            audioStreamTrack?.Stop();
            webRTCManager.RemoveAudioTrack();

            audioStreamTrack?.Dispose();
            audioStreamTrack = null;

            StartStopAudioTransmission = false;
            IsAudioTransmissionActive = false;
        }

        public void HandleMessage(SignalingMessageType messageType, string senderPeerId, string receiverPeerId, string message, int connectionCount, bool isVideoAudioSender) {

            var messageBytes = Encoding.UTF8.GetBytes($"{System.Enum.GetName(typeof(SignalingMessageType), messageType)}|{senderPeerId}|{receiverPeerId}|{message}|{connectionCount}|{isVideoAudioSender}");

            int totalChunks = Mathf.CeilToInt((float)messageBytes.Length / maxChunkSize);

            for (int i = 0; i < totalChunks; i++) {
                int offset = i * maxChunkSize;
                int size = System.Math.Min(maxChunkSize, messageBytes.Length - offset);
                byte[] chunk = new byte[size];
                System.Array.Copy(messageBytes, offset, chunk, 0, size);

                if (SignalServerMessageHandler != null) {
                    SignalServerMessageHandler.HandleMessage(senderPeerId, i, totalChunks, chunk);
                }
            }
        }

        public void ReceiveMessage(byte[] bytes) {
            webRTCManager?.HandleMessage(bytes);
        }
    }
}