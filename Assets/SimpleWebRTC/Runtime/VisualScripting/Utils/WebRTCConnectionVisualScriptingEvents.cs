#if VISUAL_SCRIPTING_INSTALLED
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Forwards the events of a SimpleWebRTC client to the Visual Scripting event units.
    /// Put it on the client GameObject (the one with the <see cref="WebRTCPeerManager"/>).
    /// </summary>
    public class WebRTCConnectionVisualScriptingEvents : MonoBehaviour {

        [SerializeField] private WebRTCPeerManager peerManager;
        [SerializeField] private DataChannel dataChannel;
        [SerializeField] private VideoReceiver videoReceiver;
        [SerializeField] private AudioReceiver audioReceiver;

        private void Awake() {
            if (peerManager == null) {
                peerManager = GetComponentInChildren<WebRTCPeerManager>(true);
            }
            if (dataChannel == null) {
                dataChannel = GetComponentInChildren<DataChannel>(true);
            }
            if (videoReceiver == null) {
                videoReceiver = GetComponentInChildren<VideoReceiver>(true);
            }
            if (audioReceiver == null) {
                audioReceiver = GetComponentInChildren<AudioReceiver>(true);
            }
        }

        private void OnEnable() {
            if (peerManager != null) {
                peerManager.SignalingStateChanged.AddListener(WebSocketConnectionChangedEvent.Trigger);
                peerManager.PeerConnected.AddListener(OnPeerConnected);
            }
            if (dataChannel != null) {
                dataChannel.ChannelOpened.AddListener(DataChannelConnectedEvent.Trigger);
                dataChannel.MessageReceived.AddListener(DataChannelMessageReceivedEvent.Trigger);
            }
            if (videoReceiver != null) {
                videoReceiver.VideoStarted.AddListener(OnVideoStarted);
            }
            if (audioReceiver != null) {
                audioReceiver.AudioStarted.AddListener(OnAudioStarted);
            }
        }

        private void OnDisable() {
            if (peerManager != null) {
                peerManager.SignalingStateChanged.RemoveListener(WebSocketConnectionChangedEvent.Trigger);
                peerManager.PeerConnected.RemoveListener(OnPeerConnected);
            }
            if (dataChannel != null) {
                dataChannel.ChannelOpened.RemoveListener(DataChannelConnectedEvent.Trigger);
                dataChannel.MessageReceived.RemoveListener(DataChannelMessageReceivedEvent.Trigger);
            }
            if (videoReceiver != null) {
                videoReceiver.VideoStarted.RemoveListener(OnVideoStarted);
            }
            if (audioReceiver != null) {
                audioReceiver.AudioStarted.RemoveListener(OnAudioStarted);
            }
        }

        private void OnPeerConnected(string peerId) {
            WebRTCConnectedEvent.Trigger();
        }

        private void OnVideoStarted(string peerId) {
            VideoTransmissionReceivedEvent.Trigger();
        }

        private void OnAudioStarted(string peerId) {
            AudioTransmissionReceivedEvent.Trigger();
        }
    }
}
#endif
