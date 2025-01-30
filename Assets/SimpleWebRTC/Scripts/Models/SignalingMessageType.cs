using System;

[Serializable]
public enum SignalingMessageType {
    NEWPEER,
    NEWPEERACK,
    OFFER,
    ANSWER,
    CANDIDATE,
    DISPOSE,
    OTHER
}