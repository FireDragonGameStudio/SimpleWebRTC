using System.Linq;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Base class for components that add a feature (data channel, video, audio, ...) to every peer session of a
    /// <see cref="WebRTCPeerManager"/>. Derive from it to build your own features.
    /// </summary>
    public abstract class PeerSessionComponent : SimpleWebRTCBehaviour {

        [Tooltip("Defaults to the WebRTCPeerManager on this GameObject or its parents.")]
        [SerializeField] protected WebRTCPeerManager peerManager;

        public WebRTCPeerManager PeerManager => peerManager;

        protected virtual void Reset() {
            peerManager = GetComponentInParent<WebRTCPeerManager>();
        }

        protected virtual void Awake() {
            if (peerManager == null) {
                peerManager = GetComponentInParent<WebRTCPeerManager>();
            }
            if (peerManager == null) {
                SimpleWebRTCLogger.LogError($"{GetType().Name} on {name} needs a {nameof(WebRTCPeerManager)}.");
            }
        }

        protected virtual void OnEnable() {
            if (peerManager == null) {
                return;
            }
            peerManager.SessionCreated += OnSessionCreated;
            peerManager.SessionClosed += OnSessionClosed;
            foreach (var session in peerManager.Sessions.ToArray()) {
                OnSessionCreated(session);
            }
        }

        protected virtual void OnDisable() {
            if (peerManager == null) {
                return;
            }
            peerManager.SessionCreated -= OnSessionCreated;
            peerManager.SessionClosed -= OnSessionClosed;
            foreach (var session in peerManager.Sessions.ToArray()) {
                OnSessionClosed(session);
            }
        }

        /// <summary>Called for every new session, before negotiation starts. Add tracks or channels here.</summary>
        protected abstract void OnSessionCreated(PeerSession session);

        /// <summary>Called before a session is disposed. Release everything created for it.</summary>
        protected abstract void OnSessionClosed(PeerSession session);
    }
}
