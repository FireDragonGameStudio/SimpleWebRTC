using UnityEngine;

namespace SimpleWebRTC {
    public static class SimpleWebRTCLogger {
        public static bool EnableLogging { get; set; }
        public static bool EnableDataChannelLogging { get; set; }

        public static void Log(object message) {
            if (EnableLogging) {
                Debug.Log(message);
            }
        }

        public static void LogWarning(object message) {
            if (EnableLogging) {
                Debug.LogWarning(message);
            }
        }

        public static void LogError(object message) {
            if (EnableLogging) {
                Debug.LogError(message);
            }
        }

        public static void LogDataChannel(object message) {
            if (EnableDataChannelLogging) {
                Debug.Log(message);
            }
        }
    }
}