using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// Collects connection statistics for every peer at a fixed interval: round trip time, bitrates, frame rate,
    /// resolution, codecs, packet loss, jitter and the network path (direct or relayed via TURN).
    /// Read them from <see cref="Stats"/>, subscribe to <see cref="StatsUpdated"/>, wire <see cref="StatsTextUpdated"/>
    /// to a UI text, or enable the on-screen overlay.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/WebRTC Stats Monitor")]
    public class WebRTCStatsMonitor : PeerSessionComponent {

        [SerializeField, Min(0.1f)] private float updateIntervalSeconds = 1f;
        [Tooltip("Draw the stats in the top left corner of the screen (IMGUI, for debugging).")]
        [SerializeField] private bool showOverlay = false;
        [SerializeField] private OverlayCorner overlayCorner = OverlayCorner.TopLeft;
        [SerializeField] private int overlayFontSize = 14;
        [SerializeField] private bool logToConsole = false;

        [Header("Events")]
        [Tooltip("Formatted stats of all peers after each update, e.g. for a UI text.")]
        public UnityEvent<string> StatsTextUpdated = new UnityEvent<string>();

        /// <summary>Raised for every peer after each update.</summary>
        public event Action<PeerStats> StatsUpdated;

        private readonly Dictionary<string, PeerStats> stats = new Dictionary<string, PeerStats>();
        // last absolute counters per stats object, to compute rates over the interval
        private readonly Dictionary<string, Counter> counters = new Dictionary<string, Counter>();
        private readonly HashSet<string> sessionIds = new HashSet<string>();
        private Coroutine pollRoutine;
        private string summary = string.Empty;
        private GUIStyle overlayStyle;

        public enum OverlayCorner {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private struct Counter {
            public double TimestampSeconds;
            public ulong Bytes;
            public ulong SecondaryBytes;
            public double JitterBufferDelay;
            public ulong JitterBufferEmitted;
            public uint Frames;
        }

        public IReadOnlyDictionary<string, PeerStats> Stats => stats;
        public string Summary => summary;

        public bool ShowOverlay {
            get => showOverlay;
            set => showOverlay = value;
        }

        public bool TryGetStats(string remotePeerId, out PeerStats peerStats) {
            return stats.TryGetValue(remotePeerId, out peerStats);
        }

        [InspectorButton("Toggle Overlay")]
        public void ToggleOverlay() {
            showOverlay = !showOverlay;
        }

        protected override void OnEnable() {
            base.OnEnable();
            pollRoutine = StartCoroutine(Poll());
        }

        protected override void OnDisable() {
            if (pollRoutine != null) {
                StopCoroutine(pollRoutine);
                pollRoutine = null;
            }
            base.OnDisable();
        }

        protected override void OnSessionCreated(PeerSession session) {
            sessionIds.Add(session.RemotePeerId);
        }

        protected override void OnSessionClosed(PeerSession session) {
            sessionIds.Remove(session.RemotePeerId);
            stats.Remove(session.RemotePeerId);
            foreach (var key in counters.Keys.Where(k => k.StartsWith(session.RemotePeerId + "/")).ToArray()) {
                counters.Remove(key);
            }
        }

        private IEnumerator Poll() {
            var wait = new WaitForSecondsRealtime(updateIntervalSeconds);
            while (true) {
                yield return wait;
                if (peerManager == null) {
                    continue;
                }
                foreach (var session in peerManager.Sessions.ToArray()) {
                    if (session.IsDisposed || !sessionIds.Contains(session.RemotePeerId)) {
                        continue;
                    }
                    var operation = session.Connection.GetStats();
                    yield return operation;
                    if (session.IsDisposed || operation.IsError || !sessionIds.Contains(session.RemotePeerId)) {
                        continue;
                    }
                    using (var report = operation.Value) {
                        var peerStats = Collect(session.RemotePeerId, report);
                        stats[session.RemotePeerId] = peerStats;
                        StatsUpdated?.Invoke(peerStats);
                    }
                }
                summary = BuildSummary();
                StatsTextUpdated?.Invoke(summary);
                if (logToConsole && stats.Count > 0) {
                    Debug.Log("[SimpleWebRTC] Stats\n" + summary);
                }
            }
        }

        private PeerStats Collect(string peerId, RTCStatsReport report) {
            var peerStats = new PeerStats { PeerId = peerId, Timestamp = DateTime.UtcNow };
            var all = report.Stats;

            // active network path
            var pair = all.Values.OfType<RTCIceCandidatePairStats>()
                .Where(p => p.state == "succeeded")
                .OrderByDescending(p => p.nominated)
                .ThenByDescending(p => p.bytesReceived + p.bytesSent)
                .FirstOrDefault();
            if (pair != null) {
                // 0 ms is a valid value on a local network, so only require answered connectivity checks
                if (pair.responsesReceived > 0) {
                    peerStats.RoundTripTimeMs = pair.currentRoundTripTime * 1000;
                }
                peerStats.AvailableOutgoingBitrateKbps = pair.availableOutgoingBitrate / 1000;
                if (all.TryGetValue(pair.localCandidateId, out var local) && local is RTCIceCandidateStats localCandidate) {
                    peerStats.LocalCandidateType = localCandidate.candidateType;
                    peerStats.Protocol = localCandidate.protocol;
                }
                if (all.TryGetValue(pair.remoteCandidateId, out var remote) && remote is RTCIceCandidateStats remoteCandidate) {
                    peerStats.RemoteCandidateType = remoteCandidate.candidateType;
                }
                peerStats.SendBitrateKbps = Rate(peerId, pair.Id, pair.Timestamp, pair.bytesSent, pair.bytesReceived, out var receiveKbps);
                peerStats.ReceiveBitrateKbps = receiveKbps;
            }

            foreach (var outbound in all.Values.OfType<RTCOutboundRTPStreamStats>()) {
                var stream = new StreamStats {
                    Kind = outbound.kind,
                    Direction = StreamDirection.Outbound,
                    Codec = CodecName(all, outbound.codecId),
                    Implementation = outbound.encoderImplementation,
                    BitrateKbps = Rate(peerId, outbound.Id, outbound.Timestamp, outbound.bytesSent, 0, out _),
                    Width = (int)outbound.frameWidth,
                    Height = (int)outbound.frameHeight,
                    FramesPerSecond = outbound.framesPerSecond,
                    QualityLimitationReason = outbound.qualityLimitationReason
                };
                // stopped senders keep their stats objects, only list streams that sent data in this interval
                if (stream.BitrateKbps > 0) {
                    peerStats.Streams.Add(stream);
                }
            }

            foreach (var inbound in all.Values.OfType<RTCInboundRTPStreamStats>()) {
                var stream = new StreamStats {
                    Kind = inbound.kind,
                    Direction = StreamDirection.Inbound,
                    Codec = CodecName(all, inbound.codecId),
                    Implementation = inbound.decoderImplementation,
                    BitrateKbps = Rate(peerId, inbound.Id, inbound.Timestamp, inbound.bytesReceived, 0, out _),
                    Width = (int)inbound.frameWidth,
                    Height = (int)inbound.frameHeight,
                    FramesPerSecond = inbound.framesPerSecond,
                    PacketsLost = inbound.packetsLost,
                    JitterMs = inbound.jitter * 1000,
                    JitterBufferDelayMs = JitterBufferDelay(peerId, inbound),
                    FramesDropped = inbound.framesDropped,
                    FreezeCount = inbound.freezeCount
                };
                if (stream.BitrateKbps > 0) {
                    peerStats.Streams.Add(stream);
                }
            }

            // fallback: round trip time reported back by the remote side for our sent streams
            if (peerStats.RoundTripTimeMs < 0) {
                var remoteInbound = all.Values.OfType<RTCRemoteInboundRtpStreamStats>().FirstOrDefault(r => r.roundTripTimeMeasurements > 0);
                if (remoteInbound != null) {
                    peerStats.RoundTripTimeMs = remoteInbound.roundTripTime * 1000;
                }
            }

            foreach (var channel in all.Values.OfType<RTCDataChannelStats>()) {
                peerStats.DataChannelMessagesSent += channel.messagesSent;
                peerStats.DataChannelMessagesReceived += channel.messagesReceived;
                peerStats.DataChannelSendKbps += Rate(peerId, channel.Id, channel.Timestamp, channel.bytesSent, channel.bytesReceived, out var channelReceiveKbps);
                peerStats.DataChannelReceiveKbps += channelReceiveKbps;
            }

            peerStats.Streams.Sort((a, b) => a.Direction != b.Direction ? a.Direction.CompareTo(b.Direction) : string.CompareOrdinal(b.Kind, a.Kind));
            return peerStats;
        }

        /// <summary>
        /// Rate in kbps since the last update for two byte counters of a stats object.
        /// </summary>
        private double Rate(string peerId, string statsId, long timestampMicroseconds, ulong bytes, ulong secondaryBytes, out double secondaryKbps) {
            var key = $"{peerId}/{statsId}";
            var now = timestampMicroseconds / 1_000_000.0;
            secondaryKbps = 0;
            counters.TryGetValue(key, out var last);
            var previous = last;
            last.TimestampSeconds = now;
            last.Bytes = bytes;
            last.SecondaryBytes = secondaryBytes;
            counters[key] = last;

            var elapsed = now - previous.TimestampSeconds;
            if (previous.TimestampSeconds <= 0 || elapsed <= 0 || bytes < previous.Bytes) {
                return 0;
            }
            if (secondaryBytes >= previous.SecondaryBytes) {
                secondaryKbps = (secondaryBytes - previous.SecondaryBytes) * 8 / elapsed / 1000;
            }
            return (bytes - previous.Bytes) * 8 / elapsed / 1000;
        }

        private double JitterBufferDelay(string peerId, RTCInboundRTPStreamStats inbound) {
            var key = $"{peerId}/{inbound.Id}/jitterbuffer";
            counters.TryGetValue(key, out var last);
            var previous = last;
            last.JitterBufferDelay = inbound.jitterBufferDelay;
            last.JitterBufferEmitted = inbound.jitterBufferEmittedCount;
            counters[key] = last;
            if (previous.JitterBufferEmitted == 0 || inbound.jitterBufferEmittedCount <= previous.JitterBufferEmitted) {
                return 0;
            }
            var emitted = inbound.jitterBufferEmittedCount - previous.JitterBufferEmitted;
            return (inbound.jitterBufferDelay - previous.JitterBufferDelay) / emitted * 1000;
        }

        private static string CodecName(IDictionary<string, RTCStats> all, string codecId) {
            if (string.IsNullOrEmpty(codecId) || !all.TryGetValue(codecId, out var value) || !(value is RTCCodecStats codec)) {
                return null;
            }
            var mimeType = codec.mimeType ?? string.Empty;
            var slash = mimeType.IndexOf('/');
            return slash >= 0 ? mimeType.Substring(slash + 1) : mimeType;
        }

        private string BuildSummary() {
            if (stats.Count == 0) {
                return peerManager != null ? $"{peerManager.LocalPeerId}: no peers connected" : string.Empty;
            }
            var builder = new StringBuilder();
            builder.Append(peerManager != null ? peerManager.LocalPeerId : name).Append(" stats");
            foreach (var peerStats in stats.Values) {
                builder.Append('\n').Append(peerStats);
            }
            return builder.ToString();
        }

        private void OnGUI() {
            if (!showOverlay || string.IsNullOrEmpty(summary)) {
                return;
            }
            overlayStyle ??= new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.UpperLeft,
                richText = false,
                wordWrap = false,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = Color.white }
            };
            overlayStyle.fontSize = overlayFontSize;
            var content = new GUIContent(summary);
            var size = overlayStyle.CalcSize(content);
            const float margin = 10;
            var x = overlayCorner == OverlayCorner.TopLeft || overlayCorner == OverlayCorner.BottomLeft ? margin : Screen.width - size.x - margin;
            var y = overlayCorner == OverlayCorner.TopLeft || overlayCorner == OverlayCorner.TopRight ? margin : Screen.height - size.y - margin;
            var rect = new Rect(x, y, size.x, size.y);

            var color = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.Label(rect, content, overlayStyle);
        }
    }
}
