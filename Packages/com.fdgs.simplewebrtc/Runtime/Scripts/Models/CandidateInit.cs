using System;
using UnityEngine;

namespace SimpleWebRTC {
    [Serializable]
    public class CandidateInit : IJsonObject<CandidateInit> {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;

        public static CandidateInit FromJSON(string jsonString) {
            return JsonUtility.FromJson<CandidateInit>(jsonString);
        }

        public string ConvertToJSON() {
            return JsonUtility.ToJson(this);
        }
    }
}