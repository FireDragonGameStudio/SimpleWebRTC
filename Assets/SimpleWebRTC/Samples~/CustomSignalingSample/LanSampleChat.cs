using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC.Samples {
    /// <summary>
    /// Minimal chat for the UDP signaling sample: shows connected peers and the last messages of the data channel.
    /// </summary>
    public class LanSampleChat : MonoBehaviour {

        [SerializeField] private WebRTCPeerManager peerManager;
        [SerializeField] private DataChannel dataChannel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI chatText;
        [SerializeField] private Button sendButton;
        [SerializeField] private int maxLines = 8;

        private readonly Queue<string> lines = new Queue<string>();
        private int counter;

        private void OnEnable() {
            dataChannel.PeerMessageReceived += OnMessage;
            sendButton.onClick.AddListener(SendHello);
        }

        private void OnDisable() {
            dataChannel.PeerMessageReceived -= OnMessage;
            sendButton.onClick.RemoveListener(SendHello);
        }

        private void Update() {
            var signaling = peerManager.Signaling != null ? peerManager.Signaling.State.ToString() : "no transport";
            statusText.text = $"You: {peerManager.LocalPeerId}   UDP signaling: {signaling}   Connected peers: {peerManager.ConnectedPeerCount}";
        }

        public void SendHello() {
            var message = $"Hello #{++counter} from {peerManager.LocalPeerId}";
            dataChannel.Send(message);
            AddLine($"(you) {message}");
        }

        private void OnMessage(string peerId, string message) {
            AddLine($"{peerId}: {message}");
        }

        private void AddLine(string line) {
            lines.Enqueue(line);
            while (lines.Count > maxLines) {
                lines.Dequeue();
            }
            chatText.text = string.Join("\n", lines);
        }
    }
}
