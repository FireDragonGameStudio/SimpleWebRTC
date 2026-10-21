using System;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// A signaling message as sent over any <see cref="ISignalingTransport"/>. Serialized as JSON, e.g.
    /// <c>{"v":2,"type":"offer","room":"default","from":"Peer-a","to":"Peer-b","payload":"{...}"}</c>.
    /// The signaling server only needs to relay messages to all other clients, filtering happens on the peers.
    /// </summary>
    [Serializable]
    public class SignalingMessage {
        public const int ProtocolVersion = 2;
        public const string Broadcast = "*";

        public int v = ProtocolVersion;
        public string type;
        public string room;
        public string from;
        public string to;
        public string payload;

        public SignalingMessage() {
        }

        public SignalingMessage(string type, string room, string from, string to, string payload = "") {
            this.type = type;
            this.room = room;
            this.from = from;
            this.to = string.IsNullOrEmpty(to) ? Broadcast : to;
            this.payload = payload ?? string.Empty;
        }

        public bool IsBroadcast => string.IsNullOrEmpty(to) || to == Broadcast;

        public bool IsAddressedTo(string peerId) => IsBroadcast || to == peerId;

        public string ToJson() {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Parses a message and returns false for anything that is not a valid v2 signaling message,
        /// so foreign traffic on a shared relay server is ignored instead of throwing.
        /// </summary>
        public static bool TryParse(string json, out SignalingMessage message) {
            message = null;
            if (string.IsNullOrEmpty(json) || json[0] != '{') {
                return false;
            }
            try {
                message = JsonUtility.FromJson<SignalingMessage>(json);
            } catch (ArgumentException) {
                return false;
            }
            return message != null
                && message.v == ProtocolVersion
                && !string.IsNullOrEmpty(message.type)
                && !string.IsNullOrEmpty(message.from);
        }

        public override string ToString() {
            return $"{type} {from} -> {to}";
        }
    }
}
