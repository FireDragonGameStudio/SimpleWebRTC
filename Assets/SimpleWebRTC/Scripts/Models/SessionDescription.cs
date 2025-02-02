using System;
using UnityEngine;

namespace SimpleWebRTC {
    [Serializable]
    public class SessionDescription : IJsonObject<SessionDescription> {
        public string SessionType;
        public string Sdp;

        public string ConvertToJSON() {
            return JsonUtility.ToJson(this);
        }

        public static SessionDescription FromJSON(string jsonString) {
            return JsonUtility.FromJson<SessionDescription>(jsonString);
        }
    }
}