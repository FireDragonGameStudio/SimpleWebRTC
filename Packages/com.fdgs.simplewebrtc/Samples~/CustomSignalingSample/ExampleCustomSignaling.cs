using UnityEngine;

namespace SimpleWebRTC.Samples {
    /// <summary>
    /// Template for your own signaling backend (REST polling, Firebase, Mirror, Netcode for GameObjects, ...).
    ///
    /// A transport only moves strings. Every string passed to <see cref="Send"/> must reach all other connected
    /// clients, and every string received from them must be handed to <see cref="SignalingTransport.ReceiveMessage"/>.
    /// The <see cref="WebRTCPeerManager"/> takes care of rooms, addressing and the WebRTC negotiation.
    ///
    /// Usage: add this component next to a WebRTCPeerManager and assign it as its signaling transport.
    /// </summary>
    public class ExampleCustomSignaling : SignalingTransport {

        [SerializeField] private string backendAddress = "https://example.com/signaling";

        public override void Connect() {
            if (State != SignalingState.Disconnected) {
                return;
            }
            SetState(SignalingState.Connecting);

            // 1. Connect to your backend here (may be asynchronous).
            // 2. Once connected, report it. SetState is safe to call from any thread.
            Debug.Log($"Connecting to {backendAddress} ...");
            SetState(SignalingState.Connected);
        }

        public override void Disconnect() {
            // Close your backend connection, then report it.
            SetState(SignalingState.Disconnected);
        }

        public override void Send(string message) {
            if (!IsConnected) {
                return;
            }
            // Deliver the message to all other clients, e.g. myBackend.Broadcast(message);
            Debug.Log($"Would send: {message}");
        }

        /// <summary>
        /// Call this from your backend's receive callback (any thread) for every message from another client.
        /// </summary>
        private void OnBackendMessage(string message) {
            ReceiveMessage(message);
        }
    }
}
