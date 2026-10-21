using TMPro;
using UnityEngine;

namespace SimpleWebRTC.Samples {
    /// <summary>
    /// Sends a chat message over the data channel every few seconds and shows the last received message.
    /// </summary>
    public class LoopbackSampleChat : MonoBehaviour {

        [SerializeField] private DataChannel dataChannel;
        [SerializeField] private TextMeshProUGUI receivedText;
        [SerializeField] private float sendIntervalSeconds = 2f;

        private int counter;

        private void OnEnable() {
            dataChannel.PeerMessageReceived += OnMessage;
            InvokeRepeating(nameof(SendChatMessage), sendIntervalSeconds, sendIntervalSeconds);
        }

        private void OnDisable() {
            dataChannel.PeerMessageReceived -= OnMessage;
            CancelInvoke();
        }

        private void SendChatMessage() {
            if (dataChannel.OpenChannelCount > 0) {
                dataChannel.Send($"Hello #{++counter} from {dataChannel.PeerManager.LocalPeerId}");
            }
        }

        private void OnMessage(string peerId, string message) {
            receivedText.text = $"{peerId}: {message}";
        }
    }
}
