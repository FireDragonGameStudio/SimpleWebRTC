using System;

namespace SimpleWebRTC {
    public static class PeerIdGenerator {
        /// <summary>
        /// Creates a short, practically unique peer id, e.g. "Peer-3f9a1c2b".
        /// </summary>
        public static string Create(string prefix = "Peer") {
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);
            return string.IsNullOrEmpty(prefix) ? id : $"{prefix}-{id}";
        }
    }
}
