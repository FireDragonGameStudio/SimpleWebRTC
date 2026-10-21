using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// The connection to one remote peer. Wraps an <see cref="RTCPeerConnection"/> and negotiates it with the
    /// "perfect negotiation" pattern, so any side can add or change tracks and data channels at any time.
    /// Created and owned by <see cref="WebRTCPeerManager"/>.
    /// </summary>
    public sealed class PeerSession : IDisposable {

        private const float initialOfferTimeoutSeconds = 2f;

        public string LocalPeerId { get; }
        public string RemotePeerId { get; }

        /// <summary>
        /// The polite peer gives way when both sides send an offer at the same time.
        /// </summary>
        public bool IsPolite { get; }

        public RTCPeerConnection Connection { get; }
        public RTCPeerConnectionState ConnectionState { get; private set; } = RTCPeerConnectionState.New;
        public bool IsConnected => ConnectionState == RTCPeerConnectionState.Connected;
        public bool IsDisposed { get; private set; }

        public event Action<PeerSession, RTCPeerConnectionState> ConnectionStateChanged;
        public event Action<PeerSession, RTCTrackEvent> TrackReceived;
        public event Action<PeerSession> Closing;

        private readonly MonoBehaviour coroutineHost;
        private readonly Action<string, string> sendSignal;
        private readonly VideoCodecPreference videoCodecPreference;

        private readonly Queue<SessionDescription> pendingDescriptions = new Queue<SessionDescription>();
        private readonly List<RTCIceCandidate> pendingCandidates = new List<RTCIceCandidate>();

        private bool makingOffer;
        private bool ignoreOffer;
        private bool srdAnswerPending;
        private bool sldGetBackStable;
        private bool negotiationScheduled;
        private bool processingDescriptions;
        private bool hasRemoteDescription;

        /// <param name="sendSignal">Sends a signaling message (type, payload) to the remote peer.</param>
        public PeerSession(string localPeerId, string remotePeerId, RTCConfiguration configuration,
            MonoBehaviour coroutineHost, Action<string, string> sendSignal, VideoCodecPreference videoCodecPreference) {

            LocalPeerId = localPeerId;
            RemotePeerId = remotePeerId;
            IsPolite = string.CompareOrdinal(localPeerId, remotePeerId) < 0;

            this.coroutineHost = coroutineHost;
            this.sendSignal = sendSignal;
            this.videoCodecPreference = videoCodecPreference;

            Connection = new RTCPeerConnection(ref configuration);
            Connection.OnIceCandidate = OnIceCandidate;
            Connection.OnConnectionStateChange = OnConnectionStateChange;
            Connection.OnTrack = e => {
                if (!IsDisposed) {
                    TrackReceived?.Invoke(this, e);
                }
            };
            Connection.OnNegotiationNeeded = () => {
                if (!IsDisposed && !negotiationScheduled) {
                    negotiationScheduled = true;
                    coroutineHost.StartCoroutine(Negotiate());
                }
            };
        }

        /// <summary>
        /// Adds a send-only transceiver. With a null track the transceiver starts inactive; use
        /// <see cref="RTCRtpSender.ReplaceTrack"/> and <see cref="RTCRtpTransceiver.Direction"/> to start sending later.
        /// </summary>
        public RTCRtpTransceiver AddSendTransceiver(TrackKind kind, MediaStreamTrack track, RTCRtpEncodingParameters[] sendEncodings = null) {
            var init = new RTCRtpTransceiverInit {
                direction = track != null ? RTCRtpTransceiverDirection.SendOnly : RTCRtpTransceiverDirection.Inactive,
                sendEncodings = sendEncodings
            };
            var transceiver = track != null ? Connection.AddTransceiver(track, init) : Connection.AddTransceiver(kind, init);
            if (kind == TrackKind.Video) {
                ApplyVideoCodecPreference(transceiver, videoCodecPreference);
            }
            return transceiver;
        }

        /// <summary>
        /// Creates a pre-negotiated data channel. Both peers must create the channel with the same id.
        /// </summary>
        public RTCDataChannel CreateNegotiatedDataChannel(string label, int id, bool ordered, int maxRetransmits) {
            var init = new RTCDataChannelInit {
                negotiated = true,
                id = id,
                ordered = ordered
            };
            if (maxRetransmits >= 0) {
                init.maxRetransmits = maxRetransmits;
            }
            return Connection.CreateDataChannel(label, init);
        }

        public void HandleDescription(SessionDescription description) {
            if (IsDisposed) {
                return;
            }
            pendingDescriptions.Enqueue(description);
            if (!processingDescriptions) {
                coroutineHost.StartCoroutine(ProcessDescriptions());
            }
        }

        public void HandleCandidate(CandidateInit candidateInit) {
            if (IsDisposed || string.IsNullOrEmpty(candidateInit.candidate)) {
                // an empty candidate marks the end of candidate gathering
                return;
            }
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit {
                candidate = candidateInit.candidate,
                sdpMid = candidateInit.sdpMid,
                sdpMLineIndex = candidateInit.sdpMLineIndex
            });

            if (!hasRemoteDescription || processingDescriptions) {
                pendingCandidates.Add(candidate);
                return;
            }
            AddCandidate(candidate);
        }

        private void AddCandidate(RTCIceCandidate candidate) {
            if (!Connection.AddIceCandidate(candidate) && !ignoreOffer) {
                SimpleWebRTCLogger.LogWarning($"{this}: could not add ICE candidate in signaling state {Connection.SignalingState}.");
            }
        }

        private void FlushPendingCandidates() {
            foreach (var candidate in pendingCandidates) {
                AddCandidate(candidate);
            }
            pendingCandidates.Clear();
        }

        private IEnumerator Negotiate() {
            // Only the impolite peer makes the first offer. An initial collision forces the polite peer to roll back
            // an offer with a different m-line layout, which libwebrtc cannot always recover from (SCTP transport).
            // The timeout covers an impolite peer that has nothing to negotiate.
            var initialOfferDeadline = Time.realtimeSinceStartup + initialOfferTimeoutSeconds;
            yield return new WaitUntil(() => IsDisposed || !IsPolite || hasRemoteDescription
                || Time.realtimeSinceStartup > initialOfferDeadline);

            // negotiationneeded can fire while an offer/answer exchange is still running, wait for it to finish
            yield return new WaitUntil(() => IsDisposed
                || (!makingOffer && !sldGetBackStable && !processingDescriptions && Connection.SignalingState == RTCSignalingState.Stable));
            negotiationScheduled = false;
            if (IsDisposed) {
                yield break;
            }

            makingOffer = true;
            var operation = Connection.SetLocalDescription();
            yield return operation;

            if (!IsDisposed) {
                if (operation.IsError) {
                    SimpleWebRTCLogger.LogError($"{this}: failed to create offer. {operation.Error.message}");
                } else if (Connection.LocalDescription.type == RTCSdpType.Offer) {
                    SimpleWebRTCLogger.Log($"{this}: sending offer.");
                    sendSignal(SignalingMessageType.Offer, new SessionDescription(Connection.LocalDescription).ConvertToJSON());
                }
            }
            makingOffer = false;
        }

        private IEnumerator ProcessDescriptions() {
            processingDescriptions = true;
            while (pendingDescriptions.Count > 0 && !IsDisposed) {
                yield return ProcessDescription(pendingDescriptions.Dequeue());
            }
            processingDescriptions = false;
            if (!IsDisposed && hasRemoteDescription) {
                FlushPendingCandidates();
            }
        }

        private IEnumerator ProcessDescription(SessionDescription sessionDescription) {
            RTCSessionDescription description;
            try {
                description = sessionDescription.ToRTCSessionDescription();
            } catch (ArgumentException) {
                SimpleWebRTCLogger.LogError($"{this}: received invalid session description type '{sessionDescription.type}'.");
                yield break;
            }

            var isOffer = description.type == RTCSdpType.Offer;
            var readyForOffer = !makingOffer
                && (Connection.SignalingState == RTCSignalingState.Stable
                    || (Connection.SignalingState == RTCSignalingState.HaveLocalOffer && srdAnswerPending));
            ignoreOffer = isOffer && !IsPolite && !readyForOffer;
            if (ignoreOffer) {
                SimpleWebRTCLogger.Log($"{this}: offer collision, ignoring remote offer (impolite peer).");
                yield break;
            }

            // a polite peer rolls back its own pending offer implicitly when applying the remote offer
            yield return new WaitWhile(() => makingOffer && !IsDisposed);
            if (IsDisposed) {
                yield break;
            }

            srdAnswerPending = !isOffer;
            var remoteOperation = Connection.SetRemoteDescription(ref description);
            yield return remoteOperation;
            srdAnswerPending = false;
            if (IsDisposed) {
                yield break;
            }

            if (remoteOperation.IsError) {
                SimpleWebRTCLogger.LogError($"{this}: failed to set remote {description.type}. {remoteOperation.Error.message}");
                yield break;
            }
            hasRemoteDescription = true;

            if (!isOffer) {
                yield break;
            }

            sldGetBackStable = true;
            var localOperation = Connection.SetLocalDescription();
            yield return localOperation;
            sldGetBackStable = false;
            if (IsDisposed) {
                yield break;
            }

            if (localOperation.IsError) {
                SimpleWebRTCLogger.LogError($"{this}: failed to create answer. {localOperation.Error.message}");
                yield break;
            }
            SimpleWebRTCLogger.Log($"{this}: sending answer.");
            sendSignal(SignalingMessageType.Answer, new SessionDescription(Connection.LocalDescription).ConvertToJSON());
        }

        private void OnIceCandidate(RTCIceCandidate candidate) {
            if (IsDisposed) {
                return;
            }
            var candidateInit = new CandidateInit {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0
            };
            sendSignal(SignalingMessageType.Candidate, candidateInit.ConvertToJSON());
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state) {
            if (IsDisposed || ConnectionState == state) {
                return;
            }
            ConnectionState = state;
            SimpleWebRTCLogger.Log($"{this}: connection state {state}.");
            ConnectionStateChanged?.Invoke(this, state);
        }

        /// <summary>
        /// Moves the preferred codec to the front of the codec list. All other codecs stay available as fallback.
        /// </summary>
        public static void ApplyVideoCodecPreference(RTCRtpTransceiver transceiver, VideoCodecPreference preference) {
            if (preference == VideoCodecPreference.Default) {
                return;
            }
            var mimeType = $"video/{preference}";
            var codecs = RTCRtpSender.GetCapabilities(TrackKind.Video).codecs;
            var preferred = codecs.Where(c => string.Equals(c.mimeType, mimeType, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (preferred.Length == 0) {
                SimpleWebRTCLogger.LogWarning($"Video codec {preference} is not supported on this platform, using default codecs.");
                return;
            }
            var ordered = preferred.Concat(codecs.Except(preferred)).ToArray();
            var error = transceiver.SetCodecPreferences(ordered);
            if (error != RTCErrorType.None) {
                SimpleWebRTCLogger.LogWarning($"Could not set video codec preference {preference}: {error}");
            }
        }

        public void Dispose() {
            if (IsDisposed) {
                return;
            }
            Closing?.Invoke(this);
            IsDisposed = true;

            ConnectionStateChanged = null;
            TrackReceived = null;
            Closing = null;
            pendingDescriptions.Clear();
            pendingCandidates.Clear();

            Connection.OnIceCandidate = null;
            Connection.OnConnectionStateChange = null;
            Connection.OnTrack = null;
            Connection.OnNegotiationNeeded = null;
            Connection.Close();
            Connection.Dispose();
        }

        public override string ToString() {
            return $"[{LocalPeerId} -> {RemotePeerId}{(IsPolite ? " polite" : string.Empty)}]";
        }
    }
}
