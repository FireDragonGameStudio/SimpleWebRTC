using System;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// Receives video from all peers and forwards it to the assigned <see cref="VideoSink"/>s.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Video Receiver")]
    public class VideoReceiver : PeerSessionComponent {

        [SerializeField] private List<VideoSink> sinks = new List<VideoSink>();

        [Header("Events")]
        [Tooltip("A remote peer started sending video. Parameter: remote peer id.")]
        public UnityEvent<string> VideoStarted = new UnityEvent<string>();
        [Tooltip("A remote peer stopped sending video or left. Parameter: remote peer id.")]
        public UnityEvent<string> VideoStopped = new UnityEvent<string>();

        /// <summary>Raised when a remote video starts or its texture changes.</summary>
        public event Action<RemoteVideo> VideoUpdated;
        /// <summary>Raised when a remote video stops.</summary>
        public event Action<RemoteVideo> VideoRemoved;

        private class Entry {
            public RemoteVideo Video;
            public RTCRtpTransceiver Transceiver;
            public VideoStreamTrack Track;
            public OnVideoReceived Handler;
            public bool IsShown;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private readonly List<Entry> changedEntries = new List<Entry>();

        public IEnumerable<RemoteVideo> Videos => entries.Values.Where(e => e.IsShown).Select(e => e.Video);
        public IList<VideoSink> Sinks => sinks;

        protected override void Reset() {
            base.Reset();
            sinks = GetComponentsInChildren<VideoSink>().ToList();
        }

        protected override void OnSessionCreated(PeerSession session) {
            session.TrackReceived += OnTrackReceived;
        }

        protected override void OnSessionClosed(PeerSession session) {
            session.TrackReceived -= OnTrackReceived;
            foreach (var entry in entries.Values.Where(e => e.Video.PeerId == session.RemotePeerId).ToArray()) {
                Hide(entry);
                entry.Track.OnVideoReceived -= entry.Handler;
                entries.Remove(entry.Video.Key);
            }
        }

        private void OnTrackReceived(PeerSession session, RTCTrackEvent e) {
            if (!(e.Track is VideoStreamTrack videoTrack)) {
                return;
            }
            var video = new RemoteVideo(session.RemotePeerId, e.Transceiver.Mid ?? videoTrack.Id);
            if (!entries.TryGetValue(video.Key, out var entry)) {
                entry = new Entry { Video = video, Transceiver = e.Transceiver, Track = videoTrack };
                entry.Handler = texture => {
                    entry.Video.Texture = texture;
                    Show(entry);
                };
                videoTrack.OnVideoReceived += entry.Handler;
                entries[video.Key] = entry;
            }
        }

        private void Update() {
            // a stopped sender sets its transceiver to inactive: hide the frozen video, show it again on restart
            changedEntries.Clear();
            foreach (var entry in entries.Values) {
                var receiving = IsReceiving(entry.Transceiver);
                if (entry.IsShown != receiving && (!receiving || entry.Video.Texture != null)) {
                    changedEntries.Add(entry);
                }
            }
            foreach (var entry in changedEntries) {
                if (entry.IsShown) {
                    Hide(entry);
                } else {
                    Show(entry);
                }
            }
        }

        private static bool IsReceiving(RTCRtpTransceiver transceiver) {
            var direction = transceiver.CurrentDirection;
            return direction == RTCRtpTransceiverDirection.RecvOnly || direction == RTCRtpTransceiverDirection.SendRecv;
        }

        private void Show(Entry entry) {
            var wasShown = entry.IsShown;
            entry.IsShown = true;
            foreach (var sink in sinks) {
                if (sink != null) {
                    sink.SetVideo(entry.Video);
                }
            }
            VideoUpdated?.Invoke(entry.Video);
            if (!wasShown) {
                SimpleWebRTCLogger.Log($"{name}: receiving video from {entry.Video.PeerId}.");
                VideoStarted?.Invoke(entry.Video.PeerId);
            }
        }

        private void Hide(Entry entry) {
            if (!entry.IsShown) {
                return;
            }
            entry.IsShown = false;
            foreach (var sink in sinks) {
                if (sink != null) {
                    sink.RemoveVideo(entry.Video);
                }
            }
            VideoRemoved?.Invoke(entry.Video);
            SimpleWebRTCLogger.Log($"{name}: video from {entry.Video.PeerId} stopped.");
            VideoStopped?.Invoke(entry.Video.PeerId);
        }
    }
}
