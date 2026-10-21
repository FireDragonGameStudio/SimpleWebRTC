using System;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// A STUN or TURN server entry. TURN servers need username and credential.
    /// </summary>
    [Serializable]
    public class IceServerConfig {
        [Tooltip("e.g. stun:stun.l.google.com:19302 or turn:turn.example.com:3478")]
        public string[] urls = { "stun:stun.l.google.com:19302" };
        public string username;
        public string credential;

        public RTCIceServer ToRTCIceServer() {
            return new RTCIceServer {
                urls = urls,
                username = username,
                credential = credential,
                credentialType = RTCIceCredentialType.Password
            };
        }
    }

    public enum VideoCodecPreference {
        Default,
        VP8,
        VP9,
        H264,
        AV1
    }
}
