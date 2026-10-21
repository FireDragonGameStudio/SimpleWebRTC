using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Displays received video. A <see cref="VideoReceiver"/> forwards every remote video to its sinks.
    /// Derive from it to show video anywhere you like.
    /// </summary>
    public abstract class VideoSink : SimpleWebRTCBehaviour {

        /// <summary>Called when a remote video starts or its texture changes (e.g. on resolution change).</summary>
        public abstract void SetVideo(RemoteVideo video);

        /// <summary>Called when a remote video stopped or its peer left.</summary>
        public abstract void RemoveVideo(RemoteVideo video);
    }

    /// <summary>
    /// A video received from a remote peer.
    /// </summary>
    public sealed class RemoteVideo {
        public string PeerId { get; }
        /// <summary>Unique per peer and stream (the transceiver mid).</summary>
        public string StreamId { get; }
        public Texture Texture { get; internal set; }

        public RemoteVideo(string peerId, string streamId) {
            PeerId = peerId;
            StreamId = streamId;
        }

        public string Key => $"{PeerId}/{StreamId}";
    }
}
