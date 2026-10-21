#if VISUAL_SCRIPTING_INSTALLED
using Unity.VisualScripting;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Resolves the SimpleWebRTC client of the scene for the Visual Scripting nodes: the GameObject stored in the
    /// scene variable "ClientConnection" with a <see cref="WebRTCPeerManager"/> and its feature components.
    /// </summary>
    public static class SimpleWebRTCSceneVariables {
        public const string ClientConnectionVariable = "ClientConnection";

        public static SimpleWebRTCClient ClientConnection {
            get {
                var clientObject = GetSceneVariableByName<GameObject>(ClientConnectionVariable);
                if (clientObject == null) {
                    SimpleWebRTCLogger.LogError($"Scene variable '{ClientConnectionVariable}' with the SimpleWebRTC client GameObject is missing.");
                }
                return new SimpleWebRTCClient(clientObject);
            }
        }

        private static T GetSceneVariableByName<T>(string variableName) {
            if (Variables.ActiveScene.IsDefined(variableName)) {
                return (T)Variables.ActiveScene.Get(variableName);
            }
            return default;
        }
    }

    /// <summary>
    /// The SimpleWebRTC components of one client GameObject, as used by the Visual Scripting nodes.
    /// </summary>
    public readonly struct SimpleWebRTCClient {
        public readonly WebRTCPeerManager PeerManager;
        public readonly DataChannel DataChannel;
        public readonly VideoSender VideoSender;
        public readonly AudioSender AudioSender;

        public SimpleWebRTCClient(GameObject clientObject) {
            PeerManager = clientObject != null ? clientObject.GetComponentInChildren<WebRTCPeerManager>(true) : null;
            DataChannel = clientObject != null ? clientObject.GetComponentInChildren<DataChannel>(true) : null;
            VideoSender = clientObject != null ? clientObject.GetComponentInChildren<VideoSender>(true) : null;
            AudioSender = clientObject != null ? clientObject.GetComponentInChildren<AudioSender>(true) : null;
        }
    }
}
#endif
