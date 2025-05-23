using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SimpleWebRTC {
    [Serializable]
    public class SessionDescription : IJsonObject<SessionDescription> {

        public string type;
        public string sdp;

        public string ConvertToJSON() {
            return JsonUtility.ToJson(this);
        }

        public string StripNonVP8CodecsFromSdp() {
            var lines = sdp.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None).ToList();
            var newLines = new List<string>();
            var allowedPt = new HashSet<string>();
            string originalMLine = null;
            int mVideoLineIndex = -1;

            // Step 1: Find VP8 payload types
            foreach (var line in lines) {
                if (line.StartsWith("a=rtpmap:", StringComparison.OrdinalIgnoreCase)) {
                    var match = Regex.Match(line, @"a=rtpmap:(\d+)\s+VP8\/90000", RegexOptions.IgnoreCase);
                    if (match.Success) {
                        allowedPt.Add(match.Groups[1].Value);
                    }
                }
            }

            // If no VP8 found, don't touch the SDP (return original to avoid breaking it)
            if (allowedPt.Count == 0) {
                SimpleWebRTCLogger.LogWarning("No VP8 codec found in SDP. Skipping SDP codec filtering.");
                return sdp;
            }

            // Step 2: Rewrite the m=video line to keep only allowed PTs
            for (int i = 0; i < lines.Count; i++) {
                var line = lines[i];
                if (line.StartsWith("m=video")) {
                    mVideoLineIndex = i;
                    originalMLine = line;
                    var parts = line.Split(' ');
                    var header = parts.Take(3); // m=video <port> <proto>
                    var filteredPayloads = parts.Skip(3).Where(pt => allowedPt.Contains(pt));
                    lines[i] = string.Join(" ", header.Concat(filteredPayloads));
                }
            }

            // Step 3: Rebuild SDP with only allowed codec lines
            foreach (var line in lines) {
                if (line.StartsWith("a=rtpmap:") || line.StartsWith("a=rtcp-fb:") || line.StartsWith("a=fmtp:")) {
                    var match = Regex.Match(line, @"a=(?:rtpmap|rtcp-fb|fmtp):(\d+)");
                    if (match.Success && !allowedPt.Contains(match.Groups[1].Value))
                        continue; // skip disallowed codec lines
                }
                newLines.Add(line);
            }

            return string.Join("\r\n", newLines);
        }

        public static SessionDescription FromJSON(string jsonString) {
            return JsonUtility.FromJson<SessionDescription>(jsonString);
        }
    }
}