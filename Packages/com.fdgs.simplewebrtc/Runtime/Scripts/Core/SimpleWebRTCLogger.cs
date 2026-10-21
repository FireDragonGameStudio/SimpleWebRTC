using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Central logger for SimpleWebRTC. Info and warnings can be muted, errors are always logged.
    /// </summary>
    public static class SimpleWebRTCLogger {
        private const string prefix = "[SimpleWebRTC] ";

        public static bool EnableLogging { get; set; } = true;
        public static bool EnableDataChannelLogging { get; set; }

        public static void Log(object message) {
            if (EnableLogging) {
                Debug.Log(prefix + message);
            }
        }

        public static void LogWarning(object message) {
            if (EnableLogging) {
                Debug.LogWarning(prefix + message);
            }
        }

        public static void LogError(object message) {
            Debug.LogError(prefix + message);
        }

        public static void LogDataChannel(object message) {
            if (EnableDataChannelLogging) {
                Debug.Log(prefix + message);
            }
        }
    }
}
