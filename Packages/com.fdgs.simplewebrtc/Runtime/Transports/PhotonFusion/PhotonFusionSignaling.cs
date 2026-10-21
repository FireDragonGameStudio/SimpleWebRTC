using System.Text;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Signaling through a running Photon Fusion 2 session, no WebSocket server needed. Start the Fusion session
    /// yourself (NetworkRunner), then call Connect on the <see cref="WebRTCPeerManager"/>. The transport becomes
    /// connected as soon as its <see cref="PhotonSignalingRelay"/> is spawned.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Signaling/Photon Fusion Signaling")]
    public class PhotonFusionSignaling : SignalingTransport {

        [SerializeField] private PhotonSignalingRelay relay;

        private bool connectRequested;

        public PhotonSignalingRelay Relay => relay;

        /// <summary>
        /// Assigns the relay at runtime, e.g. when it is spawned from a prefab. Called by the relay itself.
        /// </summary>
        public void SetRelay(PhotonSignalingRelay newRelay) {
            if (relay == newRelay) {
                return;
            }
            Unsubscribe();
            relay = newRelay;
            if (isActiveAndEnabled) {
                Subscribe();
            }
            UpdateState();
        }

        private void OnEnable() {
            Subscribe();
        }

        private void OnDisable() {
            Unsubscribe();
        }

        public override void Connect() {
            connectRequested = true;
            if (relay == null || !relay.IsReady) {
                SimpleWebRTCLogger.Log($"{name}: waiting for the Photon relay to spawn.");
            }
            UpdateState();
        }

        public override void Disconnect() {
            connectRequested = false;
            UpdateState();
        }

        public override void Send(string message) {
            if (!IsConnected) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, Photon signaling is not connected.");
                return;
            }
            relay.Send(Encoding.UTF8.GetBytes(message));
        }

        private void Subscribe() {
            if (relay == null) {
                return;
            }
            relay.ReadyChanged += OnRelayReadyChanged;
            relay.MessageReceived += OnRelayMessage;
        }

        private void Unsubscribe() {
            if (relay == null) {
                return;
            }
            relay.ReadyChanged -= OnRelayReadyChanged;
            relay.MessageReceived -= OnRelayMessage;
        }

        private void OnRelayReadyChanged(bool ready) {
            UpdateState();
        }

        private void OnRelayMessage(byte[] message) {
            if (IsConnected) {
                ReceiveMessage(Encoding.UTF8.GetString(message));
            }
        }

        private void UpdateState() {
            if (!connectRequested) {
                SetState(SignalingState.Disconnected);
            } else if (relay != null && relay.IsReady) {
                SetState(SignalingState.Connected);
            } else {
                SetState(SignalingState.Connecting);
            }
        }
    }
}
