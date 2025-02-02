using System;

namespace SimpleWebRTC {
    [Serializable]
    public enum SignalingMessageType {
        NEWPEER,
        NEWPEERACK,
        OFFER,
        ANSWER,
        CANDIDATE,
        DATA,
        DISPOSE,
        COMPLETE,
        OTHER
    }
}