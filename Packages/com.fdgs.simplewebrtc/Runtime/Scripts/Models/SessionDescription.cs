using System;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    [Serializable]
    public class SessionDescription : IJsonObject<SessionDescription> {

        public string type;
        public string sdp;

        public SessionDescription() {
        }

        public SessionDescription(RTCSessionDescription description) {
            type = description.type.ToString().ToLowerInvariant();
            sdp = description.sdp;
        }

        public RTCSessionDescription ToRTCSessionDescription() {
            return new RTCSessionDescription {
                type = (RTCSdpType)Enum.Parse(typeof(RTCSdpType), type, true),
                sdp = sdp
            };
        }

        public string ConvertToJSON() {
            return JsonUtility.ToJson(this);
        }

        public static SessionDescription FromJSON(string jsonString) {
            return JsonUtility.FromJson<SessionDescription>(jsonString);
        }
    }
}
