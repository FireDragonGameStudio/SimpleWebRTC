namespace SimpleWebRTC {
    /// <summary>
    /// Message types of the SimpleWebRTC signaling protocol (v2).
    /// </summary>
    public static class SignalingMessageType {
        /// <summary>Broadcast by a peer after its signaling transport connected.</summary>
        public const string Join = "join";
        /// <summary>Direct reply to a join, so the joining peer learns about existing peers.</summary>
        public const string Welcome = "welcome";
        /// <summary>Broadcast by a peer before it disconnects.</summary>
        public const string Leave = "leave";
        /// <summary>SDP offer, payload is a <see cref="SessionDescription"/> as JSON.</summary>
        public const string Offer = "offer";
        /// <summary>SDP answer, payload is a <see cref="SessionDescription"/> as JSON.</summary>
        public const string Answer = "answer";
        /// <summary>ICE candidate, payload is a <see cref="CandidateInit"/> as JSON.</summary>
        public const string Candidate = "candidate";
    }
}
