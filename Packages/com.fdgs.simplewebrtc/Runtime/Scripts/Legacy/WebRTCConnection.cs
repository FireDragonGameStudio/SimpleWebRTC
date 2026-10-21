using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleWebRTC {
    /// <summary>
    /// SimpleWebRTC 1.x all-in-one component. It no longer does anything and only keeps its serialized settings,
    /// so they can be converted to the 2.x components with Tools > SimpleWebRTC > Migrate Open Scenes
    /// (or the Migrate button in its inspector).
    /// </summary>
    [Obsolete("Replaced by WebRTCPeerManager, a SignalingTransport, DataChannel, VideoSender/-Receiver and AudioSender/-Receiver. Use Tools > SimpleWebRTC > Migrate Open Scenes.")]
    [AddComponentMenu("")]
    public class WebRTCConnection : MonoBehaviour {

#pragma warning disable 0414, 0649
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
        [SerializeField] private bool RenderStereo = false;
        [SerializeField] private float StereoSeparation = 0.064f;
        [SerializeField] private int OneEyeRenderSide = 1024;
        [SerializeField] private int RTDepth = 24;
        [SerializeField] private bool OneFacePerFrame = false;
        [SerializeField] private RenderTexture receivingRenderTexture;

        [SerializeField] private bool WebSocketConnectionActive;
        public UnityEvent<int> WebSocketConnectionChanged;
        public UnityEvent WebRTCConnected;
        public UnityEvent<string> DataChannelConnected;
        public UnityEvent<string> DataChannelMessageReceived;

        [SerializeField] private Vector2Int VideoResolution = new Vector2Int(1280, 720);
        [SerializeField] private Camera StreamingCamera;
        public RawImage OptionalPreviewRawImage;
        [SerializeField] private RectTransform ReceivingRawImagesParent;
        public UnityEvent VideoTransmissionReceived;

        [SerializeField] private AudioSource StreamingAudioSource;
        [SerializeField] private Transform ReceivingAudioSourceParent;
        public UnityEvent AudioTransmissionReceived;
#pragma warning restore 0414, 0649

        private void Awake() {
            Debug.LogError($"[SimpleWebRTC] '{name}' still uses the 1.x {nameof(WebRTCConnection)} component, which does nothing in 2.x. " +
                "Convert it with Tools > SimpleWebRTC > Migrate Open Scenes.", this);
        }
    }
}
