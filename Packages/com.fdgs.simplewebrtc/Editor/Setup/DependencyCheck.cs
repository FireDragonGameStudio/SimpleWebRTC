using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace SimpleWebRTC.Editor {
    /// <summary>
    /// Compiles without any SimpleWebRTC dependency, so it can tell the user what is missing instead of the
    /// package failing with compile errors. UPM packages cannot depend on git packages, so WebRTC (FDGS) and
    /// NativeWebSocket have to be installed by the user.
    /// </summary>
    internal static class DependencyCheck {
        private const string webRtcPackageUrl = "https://github.com/FireDragonGameStudio/com.unity.webrtc.fdgs.git#v3.2.0";
        private const string nativeWebSocketPackageUrl = "https://github.com/endel/NativeWebSocket.git#upm-2";
        private const string sessionKey = "SimpleWebRTC.DependencyCheck.Shown";

        [InitializeOnLoadMethod]
        private static void CheckOnLoad() {
#if !SIMPLEWEBRTC_WEBRTC
            if (Application.isBatchMode) {
                Debug.LogWarning("[SimpleWebRTC] WebRTC (FDGS) (com.fdgs.webrtc 3.2.0+) is not installed, SimpleWebRTC is not compiled.");
                return;
            }
            if (SessionState.GetBool(sessionKey, false)) {
                return;
            }
            SessionState.SetBool(sessionKey, true);
            EditorApplication.delayCall += () => {
                if (EditorUtility.DisplayDialog("SimpleWebRTC",
                    "SimpleWebRTC needs the WebRTC (FDGS) package (com.fdgs.webrtc 3.2.0 or newer). " +
                    "Until it is installed, SimpleWebRTC is not compiled.\n\nInstall it now?",
                    "Install", "Later")) {
                    InstallWebRTC();
                }
            };
#endif
        }

        [MenuItem("Tools/SimpleWebRTC/Install Dependencies/WebRTC (FDGS)")]
        private static void InstallWebRTC() {
            Install(webRtcPackageUrl);
        }

        [MenuItem("Tools/SimpleWebRTC/Install Dependencies/NativeWebSocket")]
        private static void InstallNativeWebSocket() {
            Install(nativeWebSocketPackageUrl);
        }

        [MenuItem("Tools/SimpleWebRTC/Check Dependencies")]
        private static void LogStatus() {
            var webRtc =
#if SIMPLEWEBRTC_WEBRTC
                true;
#else
                false;
#endif
            var nativeWebSocket =
#if SIMPLEWEBRTC_NATIVEWEBSOCKET
                true;
#else
                false;
#endif
            var metaVoice =
#if SIMPLEWEBRTC_META_VOICE
                true;
#else
                false;
#endif
            Debug.Log("[SimpleWebRTC] Dependencies:\n" +
                $"WebRTC (FDGS) 3.2.0+: {(webRtc ? "installed" : "MISSING (required)")}\n" +
                $"NativeWebSocket 2.x: {(nativeWebSocket ? "installed, WebSocketSignaling available" : "not installed")}\n" +
                $"Meta Voice SDK: {(metaVoice ? "installed, MetaWebSocketSignaling available" : "not installed")}\n" +
                (nativeWebSocket || metaVoice ? string.Empty : "Without a WebSocket package only LoopbackSignaling, Photon Fusion or your own transport can be used."));
        }

        private static void Install(string url) {
            Debug.Log($"[SimpleWebRTC] Installing {url}");
            Client.Add(url);
        }
    }
}
