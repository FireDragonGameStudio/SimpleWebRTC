using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleWebRTC {
    /// <summary>
    /// SimpleWebRTC 1.x component for custom signaling (Photon Fusion). It no longer does anything and only keeps
    /// its serialized settings for Tools > SimpleWebRTC > Migrate Open Scenes.
    /// </summary>
    [Obsolete("Replaced by WebRTCPeerManager with a SignalingTransport (e.g. PhotonFusionSignaling). Use Tools > SimpleWebRTC > Migrate Open Scenes.")]
    [AddComponentMenu("")]
    public class WebRTCConnection_Generic : MonoBehaviour {

#pragma warning disable 0414, 0649
        [SerializeField] private string StunServerAddress = "stun:stun.l.google.com:19302";
        [SerializeField] private string LocalPeerId = "PeerId";
        [SerializeField] private bool IsVideoAudioSender = true;
        [SerializeField] private bool IsVideoAudioReceiver = true;
        [SerializeField] private bool RandomUniquePeerId = true;
        [SerializeField] private bool ShowLogs = true;
        [SerializeField] private bool ShowDataChannelLogs = true;
        [SerializeField] private MonoBehaviour SignalServerMessageHandler;

        public UnityEvent WebRTCConnected;
        public UnityEvent<string> DataChannelConnected;
        public UnityEvent<string> DataChannelMessageReceived;

        [SerializeField] private Vector2Int VideoResolution = new Vector2Int(1280, 720);
        [SerializeField] private Camera StreamingCamera;
        public RawImage OptionalPreviewRawImage;
        public RectTransform ReceivingRawImagesParent;
        public UnityEvent VideoTransmissionReceived;

        [SerializeField] private AudioSource StreamingAudioSource;
        public Transform ReceivingAudioSourceParent;
        public UnityEvent AudioTransmissionReceived;
#pragma warning restore 0414, 0649

        private void Awake() {
            Debug.LogError($"[SimpleWebRTC] '{name}' still uses the 1.x {nameof(WebRTCConnection_Generic)} component, which does nothing in 2.x. " +
                "Convert it with Tools > SimpleWebRTC > Migrate Open Scenes.", this);
        }
    }
}
