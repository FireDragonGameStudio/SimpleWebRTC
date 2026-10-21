using System;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Sample: tests a signaling transport without WebRTC. Connects, sends a text message every second and
    /// logs everything it receives. Run it on two clients against the same server.
    /// </summary>
    public class TestWebSocketConnection : MonoBehaviour {

        [SerializeField] private SignalingTransport transport;
        [SerializeField] private float sendIntervalSeconds = 1f;

        private void Start() {
            if (transport == null) {
                transport = GetComponent<SignalingTransport>();
            }
            transport.StateChanged += OnStateChanged;
            transport.MessageReceived += OnMessageReceived;
            transport.Connect();
            InvokeRepeating(nameof(SendTestMessage), sendIntervalSeconds, sendIntervalSeconds);
        }

        private void OnDestroy() {
            if (transport != null) {
                transport.StateChanged -= OnStateChanged;
                transport.MessageReceived -= OnMessageReceived;
            }
        }

        private void SendTestMessage() {
            if (transport.IsConnected) {
                transport.Send($"plain text message from {SystemInfo.deviceName} at {DateTime.Now.ToLongTimeString()}");
            }
        }

        private void OnStateChanged(SignalingState state) {
            Debug.Log($"Signaling state: {state}");
        }

        private void OnMessageReceived(string message) {
            Debug.Log($"Received: {message}");
        }
    }
}
