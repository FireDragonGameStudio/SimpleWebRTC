using NativeWebSocket;
using UnityEngine;

namespace SimpleWebRTC {
    public class WebRTCConnectionVisualScriptingEvents : MonoBehaviour {

        [SerializeField] private WebRTCConnection connection;

        private void Awake() {
            if (!connection) {
                connection = GetComponent<WebRTCConnection>();
            }

            connection.WebSocketConnectionChanged.AddListener(WebSocketConnectionChanged);
            connection.WebRTCConnected.AddListener(WebRTCConnected);
            connection.DataChannelConnected.AddListener(DataChannelConnected);
            connection.DataChannelMessageReceived.AddListener(DataChannelMessageReceived);
            connection.VideoTransmissionReceived.AddListener(VideoTransmissionReceived);
            connection.AudioTransmissionReceived.AddListener(AudioTransmissionReceived);
        }

        private void OnDestroy() {
            connection.WebSocketConnectionChanged.RemoveListener(WebSocketConnectionChanged);
            connection.WebRTCConnected.RemoveListener(WebRTCConnected);
            connection.DataChannelConnected.RemoveListener(DataChannelConnected);
            connection.DataChannelMessageReceived.RemoveListener(DataChannelMessageReceived);
            connection.VideoTransmissionReceived.RemoveListener(VideoTransmissionReceived);
            connection.AudioTransmissionReceived.RemoveListener(AudioTransmissionReceived);
        }

        private void WebSocketConnectionChanged(WebSocketState state) {
            WebSocketConnectionChangedEvent.Trigger(state);
        }

        private void WebRTCConnected() {
            WebRTCConnectedEvent.Trigger();
        }

        private void DataChannelConnected(string senderPeerId) {
            DataChannelConnectedEvent.Trigger(senderPeerId);
        }

        private void DataChannelMessageReceived(string message) {
            DataChannelMessageReceivedEvent.Trigger(message);
        }

        private void VideoTransmissionReceived() {
            VideoTransmissionReceivedEvent.Trigger();
        }

        private void AudioTransmissionReceived() {
            AudioTransmissionReceivedEvent.Trigger();
        }
    }
}