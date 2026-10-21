using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SimpleWebRTC {
    public enum StreamDirection {
        Outbound,
        Inbound
    }

    /// <summary>
    /// Statistics of one sent or received audio/video stream, measured over the last update interval.
    /// </summary>
    [Serializable]
    public class StreamStats {
        /// <summary>"audio" or "video".</summary>
        public string Kind;
        public StreamDirection Direction;
        /// <summary>Codec name, e.g. VP8, H264, opus.</summary>
        public string Codec;
        /// <summary>Encoder or decoder in use, e.g. libvpx, NvCodec, MediaCodec (if reported).</summary>
        public string Implementation;
        public double BitrateKbps;
        public double FramesPerSecond;
        public int Width;
        public int Height;
        /// <summary>Inbound: packets lost in total.</summary>
        public long PacketsLost;
        /// <summary>Inbound: interarrival jitter in milliseconds.</summary>
        public double JitterMs;
        /// <summary>Inbound: average time a sample spent in the jitter buffer during the interval, in milliseconds.</summary>
        public double JitterBufferDelayMs;
        /// <summary>Inbound video: frames dropped in total.</summary>
        public long FramesDropped;
        /// <summary>Inbound video: number of video freezes in total.</summary>
        public long FreezeCount;
        /// <summary>Outbound video: why the encoder reduces quality ("none", "bandwidth", "cpu", "other").</summary>
        public string QualityLimitationReason;

        public bool IsVideo => Kind == "video";

        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append(Direction == StreamDirection.Outbound ? "↑ " : "↓ ");
            builder.Append(Kind).Append(' ').Append(string.IsNullOrEmpty(Codec) ? "?" : Codec);
            if (!string.IsNullOrEmpty(Implementation)) {
                builder.Append(" (").Append(Implementation).Append(')');
            }
            builder.Append(' ').Append(PeerStats.FormatBitrate(BitrateKbps));
            if (IsVideo) {
                builder.Append(string.Format(CultureInfo.InvariantCulture, " {0}x{1} {2:0} fps", Width, Height, FramesPerSecond));
            }
            if (Direction == StreamDirection.Inbound) {
                builder.Append(string.Format(CultureInfo.InvariantCulture, " lost {0} jitter {1:0} ms", PacketsLost, JitterMs));
                if (JitterBufferDelayMs > 0) {
                    builder.Append(string.Format(CultureInfo.InvariantCulture, " buffer {0:0} ms", JitterBufferDelayMs));
                }
                if (IsVideo && (FramesDropped > 0 || FreezeCount > 0)) {
                    builder.Append($" dropped {FramesDropped} freezes {FreezeCount}");
                }
            } else if (IsVideo && !string.IsNullOrEmpty(QualityLimitationReason) && QualityLimitationReason != "none") {
                builder.Append(" limited by ").Append(QualityLimitationReason);
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Statistics of the connection to one remote peer, measured over the last update interval.
    /// </summary>
    [Serializable]
    public class PeerStats {
        public string PeerId;
        public DateTime Timestamp;
        /// <summary>Current round trip time of the active network path in milliseconds (-1 if unknown).</summary>
        public double RoundTripTimeMs = -1;
        /// <summary>Estimated bandwidth available for sending, in kbps (0 if unknown).</summary>
        public double AvailableOutgoingBitrateKbps;
        /// <summary>Candidate types of the active path: host, srflx (via STUN), prflx or relay (via TURN).</summary>
        public string LocalCandidateType;
        public string RemoteCandidateType;
        /// <summary>Network protocol of the active path (udp or tcp).</summary>
        public string Protocol;
        public double SendBitrateKbps;
        public double ReceiveBitrateKbps;
        public double DataChannelSendKbps;
        public double DataChannelReceiveKbps;
        public long DataChannelMessagesSent;
        public long DataChannelMessagesReceived;
        public List<StreamStats> Streams = new List<StreamStats>();

        /// <summary>True when the connection goes through a TURN server.</summary>
        public bool IsRelayed => LocalCandidateType == "relay" || RemoteCandidateType == "relay";

        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append(PeerId).Append(": ");
            builder.Append(RoundTripTimeMs >= 0 ? string.Format(CultureInfo.InvariantCulture, "RTT {0:0.#} ms", RoundTripTimeMs) : "RTT ?");
            builder.Append(" | ↑ ").Append(FormatBitrate(SendBitrateKbps)).Append(" ↓ ").Append(FormatBitrate(ReceiveBitrateKbps));
            if (AvailableOutgoingBitrateKbps > 0) {
                builder.Append(" | available ↑ ").Append(FormatBitrate(AvailableOutgoingBitrateKbps));
            }
            if (!string.IsNullOrEmpty(LocalCandidateType)) {
                builder.Append(" | ").Append(LocalCandidateType).Append('/').Append(RemoteCandidateType);
                if (!string.IsNullOrEmpty(Protocol)) {
                    builder.Append(' ').Append(Protocol);
                }
                if (IsRelayed) {
                    builder.Append(" (TURN)");
                }
            }
            foreach (var stream in Streams) {
                builder.Append("\n  ").Append(stream);
            }
            if (DataChannelMessagesSent > 0 || DataChannelMessagesReceived > 0) {
                builder.Append("\n  data ↑ ").Append(DataChannelMessagesSent).Append(" msgs ").Append(FormatBitrate(DataChannelSendKbps))
                    .Append(" ↓ ").Append(DataChannelMessagesReceived).Append(" msgs ").Append(FormatBitrate(DataChannelReceiveKbps));
            }
            return builder.ToString();
        }

        public static string FormatBitrate(double kbps) {
            return kbps >= 1000
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} Mbps", kbps / 1000)
                : string.Format(CultureInfo.InvariantCulture, "{0:0} kbps", kbps);
        }
    }
}
