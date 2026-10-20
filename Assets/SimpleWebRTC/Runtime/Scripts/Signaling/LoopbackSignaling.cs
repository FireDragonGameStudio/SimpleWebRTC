using System.Collections.Generic;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// In-process signaling: relays messages between all loopback transports with the same channel name.
    /// No server needed, ideal for testing and for multiple peers in one scene.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Signaling/Loopback Signaling")]
    public class LoopbackSignaling : SignalingTransport {

        private static readonly Dictionary<string, List<LoopbackSignaling>> channels = new Dictionary<string, List<LoopbackSignaling>>();

        [SerializeField] private string channel = "default";

        public string Channel {
            get => channel;
            set => channel = value;
        }

        public override void Connect() {
            if (State != SignalingState.Disconnected) {
                return;
            }
            if (!channels.TryGetValue(channel, out var members)) {
                members = new List<LoopbackSignaling>();
                channels[channel] = members;
            }
            members.Add(this);
            SetState(SignalingState.Connected);
        }

        public override void Disconnect() {
            if (channels.TryGetValue(channel, out var members)) {
                members.Remove(this);
            }
            SetState(SignalingState.Disconnected);
        }

        public override void Send(string message) {
            if (!channels.TryGetValue(channel, out var members) || !members.Contains(this)) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, loopback signaling is not connected.");
                return;
            }
            foreach (var member in members.ToArray()) {
                if (member != this) {
                    // deliver asynchronously like a real network would
                    MainThreadDispatcher.Post(() => {
                        if (member != null && member.IsConnected) {
                            member.ReceiveMessage(message);
                        }
                    });
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            channels.Clear();
        }
    }
}
