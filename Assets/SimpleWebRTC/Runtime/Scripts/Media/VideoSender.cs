using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleWebRTC {
    /// <summary>
    /// Sends the video of a <see cref="VideoSource"/> to all peers. Every peer gets its own transceiver, so peers
    /// joining later receive the stream too. Starting and stopping only swaps the track, no reconnect needed.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Video Sender")]
    public class VideoSender : PeerSessionComponent {

        [SerializeField] private VideoSource source;
        [Tooltip("Start streaming as soon as the component is enabled.")]
        [SerializeField] private bool streamOnEnable = false;
        [Tooltip("Optional local preview of the sent video.")]
        [SerializeField] private RawImage previewImage;

        [Header("Encoding")]
        [Tooltip("Maximum bitrate in kbps, 0 = let WebRTC decide.")]
        [SerializeField] private int maxBitrateKbps = 0;
        [Tooltip("Maximum framerate, 0 = unlimited.")]
        [SerializeField] private int maxFramerate = 0;

        [Header("Events")]
        public UnityEvent StreamingStarted = new UnityEvent();
        public UnityEvent StreamingStopped = new UnityEvent();

        private readonly Dictionary<string, RTCRtpTransceiver> transceivers = new Dictionary<string, RTCRtpTransceiver>();
        private VideoStreamTrack track;
        private Coroutine startRoutine;

        public VideoSource Source {
            get => source;
            set => source = value;
        }

        public bool IsStreaming => track != null;
        public VideoStreamTrack Track => track;

        protected override void Reset() {
            base.Reset();
            source = GetComponent<VideoSource>();
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (streamOnEnable) {
                StartStreaming();
            }
        }

        protected override void OnDisable() {
            StopStreaming();
            base.OnDisable();
        }

        [InspectorButton]
        public void StartStreaming() {
            if (IsStreaming || startRoutine != null) {
                return;
            }
            if (source == null) {
                SimpleWebRTCLogger.LogError($"{name}: no {nameof(VideoSource)} assigned to {nameof(VideoSender)}.");
                return;
            }
            startRoutine = StartCoroutine(StartStreamingRoutine());
        }

        [InspectorButton]
        public void StopStreaming() {
            if (startRoutine != null) {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }
            if (!IsStreaming) {
                return;
            }
            foreach (var transceiver in transceivers.Values) {
                transceiver.Sender.ReplaceTrack(null);
                transceiver.Direction = RTCRtpTransceiverDirection.Inactive;
            }
            if (previewImage != null) {
                previewImage.texture = null;
            }
            source.ReleaseTrack(track);
            track = null;
            SimpleWebRTCLogger.Log($"{name}: video streaming stopped.");
            StreamingStopped?.Invoke();
        }

        public void ToggleStreaming() {
            if (IsStreaming) {
                StopStreaming();
            } else {
                StartStreaming();
            }
        }

        private IEnumerator StartStreamingRoutine() {
            VideoStreamTrack createdTrack = null;
            yield return source.CreateTrack(t => createdTrack = t);
            startRoutine = null;
            if (createdTrack == null) {
                yield break;
            }

            track = createdTrack;
            foreach (var transceiver in transceivers.Values) {
                transceiver.Sender.ReplaceTrack(track);
                transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
            }
            if (previewImage != null) {
                previewImage.texture = track.Texture;
            }
            SimpleWebRTCLogger.Log($"{name}: video streaming started.");
            StreamingStarted?.Invoke();
        }

        protected override void OnSessionCreated(PeerSession session) {
            if (transceivers.ContainsKey(session.RemotePeerId)) {
                return;
            }
            transceivers[session.RemotePeerId] = session.AddSendTransceiver(TrackKind.Video, track, CreateEncodings());
        }

        protected override void OnSessionClosed(PeerSession session) {
            transceivers.Remove(session.RemotePeerId);
        }

        private RTCRtpEncodingParameters[] CreateEncodings() {
            if (maxBitrateKbps <= 0 && maxFramerate <= 0) {
                return null;
            }
            var encoding = new RTCRtpEncodingParameters { active = true };
            if (maxBitrateKbps > 0) {
                encoding.maxBitrate = (ulong)maxBitrateKbps * 1000;
            }
            if (maxFramerate > 0) {
                encoding.maxFramerate = (uint)maxFramerate;
            }
            return new[] { encoding };
        }
    }
}
