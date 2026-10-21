using System;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// The hub of a SimpleWebRTC client. Joins a room over a <see cref="SignalingTransport"/> and keeps one
    /// <see cref="PeerSession"/> per remote peer (full mesh). Features like data channels, video and audio are
    /// separate components on the same GameObject (or children) that hook into the sessions.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SimpleWebRTC/WebRTC Peer Manager")]
    public class WebRTCPeerManager : SimpleWebRTCBehaviour {

        [Header("Signaling")]
        [SerializeField] private SignalingTransport signaling;
        [Tooltip("Only peers in the same room connect to each other.")]
        [SerializeField] private string room = "default";
        [SerializeField] private bool connectOnStart = false;

        [Header("Local Peer")]
        [SerializeField] private string localPeerId = "Peer";
        [Tooltip("Appends a random suffix to the peer id, so every client is unique.")]
        [SerializeField] private bool appendRandomSuffix = true;

        [Header("WebRTC")]
        [SerializeField] private List<IceServerConfig> iceServers = new List<IceServerConfig> { new IceServerConfig() };
        [Tooltip("Preferred codec for sent video. Other codecs stay available as fallback.")]
        [SerializeField] private VideoCodecPreference preferredVideoCodec = VideoCodecPreference.VP8;

        [Header("Logging")]
        [SerializeField] private bool showLogs = true;
        [SerializeField] private bool showDataChannelLogs = false;

        [Header("Events")]
        public UnityEvent<SignalingState> SignalingStateChanged = new UnityEvent<SignalingState>();
        [Tooltip("A remote peer joined the room, the connection is being negotiated.")]
        public UnityEvent<string> PeerJoined = new UnityEvent<string>();
        [Tooltip("The WebRTC connection to a remote peer is established.")]
        public UnityEvent<string> PeerConnected = new UnityEvent<string>();
        [Tooltip("A remote peer left or its connection was closed.")]
        public UnityEvent<string> PeerLeft = new UnityEvent<string>();

        /// <summary>Raised when a session to a remote peer is created, before negotiation starts.</summary>
        public event Action<PeerSession> SessionCreated;
        /// <summary>Raised right before a session is disposed.</summary>
        public event Action<PeerSession> SessionClosed;

        private readonly Dictionary<string, PeerSession> sessions = new Dictionary<string, PeerSession>();
        // generated at runtime from localPeerId, never written back into the serialized field
        private string runtimePeerId;
        private bool joinRequested;

        public string LocalPeerId {
            get {
                InitializePeerId();
                return runtimePeerId;
            }
        }
        public string Room => room;
        public SignalingTransport Signaling => signaling;
        public bool IsSignalingConnected => signaling != null && signaling.IsConnected;
        public IReadOnlyCollection<PeerSession> Sessions => sessions.Values;
        public int ConnectedPeerCount => sessions.Values.Count(s => s.IsConnected);

        private void Awake() {
            ApplyLogSettings();
            InitializePeerId();
        }

        private void OnEnable() {
            WebRTCUpdateRunner.Acquire();
            Subscribe();
        }

        private void Start() {
            if (connectOnStart) {
                Connect();
            }
        }

        private void OnDisable() {
            Disconnect();
            Unsubscribe();
            WebRTCUpdateRunner.Release();
        }

        private void OnValidate() {
            ApplyLogSettings();
        }

        /// <summary>
        /// Connects the signaling transport (if needed) and joins the room.
        /// </summary>
        [InspectorButton]
        public void Connect() {
            if (signaling == null) {
                SimpleWebRTCLogger.LogError($"{name}: no signaling transport assigned to {nameof(WebRTCPeerManager)}.");
                return;
            }
            if (joinRequested && signaling.State != SignalingState.Disconnected) {
                // already joined or joining
                return;
            }
            joinRequested = true;
            if (signaling.IsConnected) {
                SendSignal(SignalingMessageType.Join, SignalingMessage.Broadcast);
            } else if (signaling.State == SignalingState.Disconnected) {
                signaling.Connect();
            }
        }

        /// <summary>
        /// Leaves the room, closes all peer connections and disconnects the signaling transport.
        /// </summary>
        [InspectorButton]
        public void Disconnect() {
            joinRequested = false;
            if (IsSignalingConnected) {
                SendSignal(SignalingMessageType.Leave, SignalingMessage.Broadcast);
            }
            CloseAllSessions();
            if (signaling != null && signaling.State != SignalingState.Disconnected) {
                signaling.Disconnect();
            }
        }

        /// <summary>
        /// Sets the local peer id. Only possible while not connected.
        /// </summary>
        public void SetLocalPeerId(string peerId) {
            if (IsSignalingConnected) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot change the peer id while connected.");
                return;
            }
            localPeerId = peerId;
            runtimePeerId = null;
            InitializePeerId();
        }

        public void SetRoom(string roomName) {
            if (IsSignalingConnected) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot change the room while connected.");
                return;
            }
            room = roomName;
        }

        /// <summary>
        /// Swaps the signaling transport at runtime, e.g. after creating a Photon session. Only while disconnected.
        /// </summary>
        public void SetSignaling(SignalingTransport transport) {
            if (IsSignalingConnected) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot change the signaling transport while connected.");
                return;
            }
            Unsubscribe();
            signaling = transport;
            if (isActiveAndEnabled) {
                Subscribe();
            }
        }

        public bool TryGetSession(string remotePeerId, out PeerSession session) {
            return sessions.TryGetValue(remotePeerId, out session);
        }

        private void InitializePeerId() {
            if (runtimePeerId != null) {
                return;
            }
            if (string.IsNullOrWhiteSpace(localPeerId)) {
                runtimePeerId = PeerIdGenerator.Create();
            } else {
                runtimePeerId = appendRandomSuffix ? PeerIdGenerator.Create(localPeerId) : localPeerId;
            }
        }

        private void ApplyLogSettings() {
            SimpleWebRTCLogger.EnableLogging = showLogs;
            SimpleWebRTCLogger.EnableDataChannelLogging = showDataChannelLogs;
        }

        private void Subscribe() {
            if (signaling == null) {
                return;
            }
            signaling.StateChanged += OnSignalingStateChanged;
            signaling.MessageReceived += OnSignalingMessage;
        }

        private void Unsubscribe() {
            if (signaling == null) {
                return;
            }
            signaling.StateChanged -= OnSignalingStateChanged;
            signaling.MessageReceived -= OnSignalingMessage;
        }

        private void OnSignalingStateChanged(SignalingState state) {
            if (state == SignalingState.Connected && joinRequested) {
                SendSignal(SignalingMessageType.Join, SignalingMessage.Broadcast);
            }
            if (state == SignalingState.Disconnected) {
                // peer connections can outlive signaling, but without it they cannot renegotiate or reconnect
                CloseAllSessions();
            }
            SignalingStateChanged?.Invoke(state);
        }

        private void OnSignalingMessage(string raw) {
            if (!SignalingMessage.TryParse(raw, out var message)) {
                SimpleWebRTCLogger.Log($"{name}: ignoring unknown signaling message: {raw}");
                return;
            }
            if (message.from == LocalPeerId || message.room != room || !message.IsAddressedTo(LocalPeerId)) {
                return;
            }

            PeerSession session;
            switch (message.type) {
                case SignalingMessageType.Join:
                    // a rejoining peer (e.g. after a reconnect) gets a fresh session
                    CloseSession(message.from);
                    CreateSession(message.from);
                    SendSignal(SignalingMessageType.Welcome, message.from);
                    break;
                case SignalingMessageType.Welcome:
                    if (!sessions.ContainsKey(message.from)) {
                        CreateSession(message.from);
                    }
                    break;
                case SignalingMessageType.Leave:
                    CloseSession(message.from);
                    break;
                case SignalingMessageType.Offer:
                case SignalingMessageType.Answer:
                    if (!sessions.TryGetValue(message.from, out session)) {
                        if (message.type == SignalingMessageType.Answer) {
                            return;
                        }
                        session = CreateSession(message.from);
                    }
                    session.HandleDescription(SessionDescription.FromJSON(message.payload));
                    break;
                case SignalingMessageType.Candidate:
                    if (sessions.TryGetValue(message.from, out session)) {
                        session.HandleCandidate(CandidateInit.FromJSON(message.payload));
                    }
                    break;
                default:
                    SimpleWebRTCLogger.Log($"{name}: ignoring signaling message of type {message.type}.");
                    break;
            }
        }

        private PeerSession CreateSession(string remotePeerId) {
            var configuration = new RTCConfiguration {
                iceServers = iceServers
                    .Where(server => server.urls != null && server.urls.Any(url => !string.IsNullOrWhiteSpace(url)))
                    .Select(server => server.ToRTCIceServer())
                    .ToArray()
            };
            var session = new PeerSession(LocalPeerId, remotePeerId, configuration, this,
                (type, payload) => SendSignal(type, remotePeerId, payload), preferredVideoCodec);

            session.ConnectionStateChanged += OnSessionStateChanged;
            sessions[remotePeerId] = session;

            SimpleWebRTCLogger.Log($"{name}: created session {session}.");
            SessionCreated?.Invoke(session);
            PeerJoined?.Invoke(remotePeerId);
            return session;
        }

        private void OnSessionStateChanged(PeerSession session, RTCPeerConnectionState state) {
            switch (state) {
                case RTCPeerConnectionState.Connected:
                    PeerConnected?.Invoke(session.RemotePeerId);
                    break;
                case RTCPeerConnectionState.Failed:
                case RTCPeerConnectionState.Closed:
                    // never dispose a peer connection from inside its own callback
                    MainThreadDispatcher.Post(() => {
                        if (sessions.TryGetValue(session.RemotePeerId, out var current) && current == session) {
                            CloseSession(session.RemotePeerId);
                        }
                    });
                    break;
            }
        }

        private void CloseSession(string remotePeerId) {
            if (!sessions.TryGetValue(remotePeerId, out var session)) {
                return;
            }
            sessions.Remove(remotePeerId);
            SessionClosed?.Invoke(session);
            session.Dispose();
            SimpleWebRTCLogger.Log($"{name}: closed session to {remotePeerId}.");
            PeerLeft?.Invoke(remotePeerId);
        }

        private void CloseAllSessions() {
            foreach (var remotePeerId in sessions.Keys.ToArray()) {
                CloseSession(remotePeerId);
            }
        }

        private void SendSignal(string type, string to, string payload = "") {
            if (!IsSignalingConnected) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send {type}, signaling is not connected.");
                return;
            }
            signaling.Send(new SignalingMessage(type, room, LocalPeerId, to, payload).ToJson());
        }
    }
}
