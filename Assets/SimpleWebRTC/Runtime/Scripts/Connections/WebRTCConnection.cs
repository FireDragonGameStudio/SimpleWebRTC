#if !USE_NATIVEWEBSOCKET
using Meta.Net.NativeWebSocket;
#else
using NativeWebSocket;
#endif
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
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
        public bool IsImmersiveSetupActive => UseImmersiveSetup;
        public Camera VideoStreamingCamera => StreamingCamera;
        public bool IsSender => IsVideoAudioSender;
        public bool IsReceiver => IsVideoAudioReceiver;
        public bool ExperimentalSupportFor6DOF => experimentalSupportFor6DOF;
        public Transform ExperimentalSpectatorCam6DOF => experimentalSpectatorCam6DOF;

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

        [Header("Immersive Setup")]
        [SerializeField] private bool UseImmersiveSetup = false;
        [SerializeField] private bool experimentalSupportFor6DOF = false;
        [SerializeField] private Transform experimentalSpectatorCam6DOF;
        [Header("Immersive Sender")]
        [SerializeField] private bool RenderStereo = false;
        [SerializeField] private float StereoSeparation = 0.064f;
        [SerializeField] private int OneEyeRenderSide = 1024;
        [SerializeField] private RenderTextureDepth RTDepth = RenderTextureDepth.Depth24;
        [SerializeField] private bool OneFacePerFrame = false;
        [Header("Immersive Receiver")]
        [SerializeField] private RenderTexture receivingRenderTexture;

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
        [SerializeField] private RectTransform ReceivingRawImagesParent;
        public UnityEvent VideoTransmissionReceived;

        [Header("Audio Transmission")]
        [SerializeField] private bool StartStopAudioTransmission = false;
        [SerializeField] private AudioSource StreamingAudioSource;
        [SerializeField] private Transform ReceivingAudioSourceParent;
        public UnityEvent AudioTransmissionReceived;

        private WebRTCManager webRTCManager;
        private VideoStreamTrack videoStreamTrack;
        private AudioStreamTrack audioStreamTrack;

        private RenderTexture cubemapLeftEye;
        private RenderTexture cubemapRightEye;
        private RenderTexture videoEquirect;

        private int faceToRender;
        private int faceMask = 63;

        // handle creation and destruction parts on monobehaviour
        private bool createVideoReceiver;
        private string videoReceiverSenderPeerId;
        private bool createAudioReceiver;
        private string audioReceiverSenderPeerId;

        private List<GameObject> tempDestroyGameObjectRefs = new List<GameObject>();

        private bool createOffer;
        private bool createAnswer;
        private string answerSenderPeerId, answerJson;

        private bool startWebRTCUpdate;
        private bool stopWebRTCUpdate;
        private bool stopAllCoroutines;

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

            // setup immersive if selected
            if (UseImmersiveSetup) {
                if (IsVideoAudioSender) {
                    cubemapLeftEye = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                    cubemapLeftEye.dimension = TextureDimension.Cube;
                    cubemapLeftEye.hideFlags = HideFlags.HideAndDontSave;
                    cubemapLeftEye.Create();

                    if (RenderStereo) {
                        cubemapRightEye = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                        cubemapRightEye.dimension = TextureDimension.Cube;
                        cubemapRightEye.hideFlags = HideFlags.HideAndDontSave;
                        cubemapRightEye.Create();
                        //equirect height should be twice the height of cubemap if we render in stereo
                        videoEquirect = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide * 2, (int)RTDepth, RenderTextureFormat.BGRA32);
                    } else {
                        videoEquirect = new RenderTexture(OneEyeRenderSide, OneEyeRenderSide, (int)RTDepth, RenderTextureFormat.BGRA32);
                    }
                    videoEquirect.hideFlags = HideFlags.HideAndDontSave;
                    videoEquirect.Create();
                }
            }
        }

        private void Update() {

#if USE_NATIVEWEBSOCKET && (!UNITY_WEBGL || UNITY_EDITOR)
            webRTCManager.DispatchMessageQueue();
#endif

            if (SimpleWebRTCLogger.EnableLogging != ShowLogs) {
                SimpleWebRTCLogger.EnableLogging = ShowLogs;
            }

            CreateVideoReceiver();
            CreateAudioReceiver();
            DestroyCachedGameObjects();
            StartWebRTCUpdate();
            StopWebRTCUpdate();
            CreateOffer();
            CreateAnswer();

            if (stopAllCoroutines) {
                stopAllCoroutines = false;
                StopAllCoroutines();
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
                WebSocketTestMessage();
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

            if (IsImmersiveSetupActive && IsVideoAudioReceiver) {
                if (webRTCManager.ImmersiveVideoTexture != null) {
                    Graphics.Blit(webRTCManager.ImmersiveVideoTexture, receivingRenderTexture);
                }
            }
        }

        private void LateUpdate() {
            if (UseImmersiveSetup && IsVideoAudioSender) {
                if (OneFacePerFrame) {
                    faceToRender = Time.frameCount % 6;
                    faceMask = 1 << faceToRender;
                }
                if (RenderStereo) {
                    // render left and right eye for IPD StereoSeparation
                    StreamingCamera.stereoSeparation = StereoSeparation;

                    // render both eyes for stereo view
                    StreamingCamera.RenderToCubemap(cubemapRightEye, faceMask, Camera.MonoOrStereoscopicEye.Right);
                    StreamingCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);

                    // convert into equirect rendertexture for streaming
                    cubemapLeftEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Left);
                    cubemapRightEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Right);

                } else {
                    StreamingCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);
                    cubemapLeftEye.ConvertToEquirect(videoEquirect, Camera.MonoOrStereoscopicEye.Mono);
                }
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

            // release rendertextures to free memory
            cubemapLeftEye?.Release();
            if (RenderStereo) {
                cubemapRightEye?.Release();
            }
            videoEquirect?.Release();
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

        public void WebSocketTestMessage() {
            WebSocketTestMessage(webSocketTestMessage);
        }

        public void WebSocketTestMessage(string message) {
            webRTCManager.SendWebSocketTestMessage($"{message} from {LocalPeerId}");
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

            if (UseImmersiveSetup) {
                videoStreamTrack = new VideoStreamTrack(videoEquirect);
            } else {
                videoStreamTrack = StreamingCamera.CaptureStreamTrack(VideoResolution.x, VideoResolution.y);
            }

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

        public void CreateVideoReceiverGameObject(string senderPeerId) {
            videoReceiverSenderPeerId = senderPeerId;
            createVideoReceiver = true;
        }

        private void CreateVideoReceiver() {
            if (createVideoReceiver) {
                createVideoReceiver = false;

                // create new video receiver gameobject
                var receivingRawImage = new GameObject().AddComponent<RawImage>();
                receivingRawImage.name = $"{videoReceiverSenderPeerId}-Receiving-RawImage";
                receivingRawImage.rectTransform.SetParent(ReceivingRawImagesParent, false);
                receivingRawImage.rectTransform.localScale = Vector3.one;
                receivingRawImage.rectTransform.anchorMin = Vector2.zero;
                receivingRawImage.rectTransform.anchorMax = Vector2.one;
                receivingRawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                receivingRawImage.rectTransform.sizeDelta = Vector2.zero;
                webRTCManager.VideoReceivers[videoReceiverSenderPeerId] = receivingRawImage;
            }
        }

        public void CreateAudioReceiverGameObject(string senderPeerId) {
            audioReceiverSenderPeerId = senderPeerId;
            createAudioReceiver = true;
        }

        private void CreateAudioReceiver() {
            if (createAudioReceiver) {
                createAudioReceiver = false;
                var receivingAudioSource = new GameObject().AddComponent<AudioSource>();
                receivingAudioSource.name = $"{audioReceiverSenderPeerId}-Receiving-AudioSource";
                receivingAudioSource.transform.SetParent(ReceivingAudioSourceParent);
                webRTCManager.AudioReceivers[audioReceiverSenderPeerId] = receivingAudioSource;
            }
        }

        public void DestroyVideoReceiverGameObject(string senderPeerId, bool removeFromReceivers = false) {
            tempDestroyGameObjectRefs.Add(webRTCManager.VideoReceivers[senderPeerId].gameObject);
            if (removeFromReceivers) {
                webRTCManager.VideoReceivers.Remove(senderPeerId);
            }
        }

        public void DestroyAudioReceiverGameObject(string senderPeerId, bool removeFromReceivers = false) {
            tempDestroyGameObjectRefs.Add(webRTCManager.AudioReceivers[senderPeerId].gameObject);
            if (removeFromReceivers) {
                webRTCManager.AudioReceivers.Remove(senderPeerId);
            }
        }

        private void DestroyCachedGameObjects() {
            if (tempDestroyGameObjectRefs.Count > 0) {
                foreach (var cachedGameObject in tempDestroyGameObjectRefs) {
                    if (cachedGameObject != null) {
                        Destroy(cachedGameObject);
                    }
                }
            }
        }

        public void CreateAnswerCoroutine(string senderPeerId, string answerMessageJson) {
            answerSenderPeerId = senderPeerId;
            answerJson = answerMessageJson;
            createAnswer = true;
        }

        private void CreateAnswer() {
            if (createAnswer) {
                createAnswer = false;
                StartCoroutine(webRTCManager.CreateAnswer(answerSenderPeerId, answerJson));
            }
        }

        public void CreateOfferCoroutine() {
            createOffer = true;
        }

        private void CreateOffer() {
            if (createOffer) {
                createOffer = false;
                StartCoroutine(webRTCManager.CreateOffer());
            }
        }

        private void StartWebRTCUpdate() {
            if (startWebRTCUpdate) {
                startWebRTCUpdate = false;
                StartCoroutine(WebRTC.Update());
            }
        }

        private void StopWebRTCUpdate() {
            if (stopWebRTCUpdate) {
                stopWebRTCUpdate = false;
                StopCoroutine(WebRTC.Update());
            }
        }

        public void StartWebRTUpdateCoroutine() {
            startWebRTCUpdate = true;
        }

        public void StopWebRTCUpdateCoroutine() {
            stopWebRTCUpdate = true;
        }

        public void StopAllCoroutinesManually() {
            stopAllCoroutines = true;
        }
    }
}