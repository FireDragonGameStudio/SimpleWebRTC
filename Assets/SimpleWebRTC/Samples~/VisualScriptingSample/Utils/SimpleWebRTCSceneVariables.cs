using Unity.VisualScripting;
using UnityEngine;

namespace SimpleWebRTC {
    public static class SimpleWebRTCSceneVariables {
        public static WebRTCConnection ClientConnection => GetSceneVariableByName<GameObject>("ClientConnection").GetComponent<WebRTCConnection>();

        private static bool CheckSceneVariable(string variableName) {
            return Variables.ActiveScene.IsDefined(variableName);
        }

        private static T GetSceneVariableByName<T>(string variableName) {
            if (CheckSceneVariable(variableName)) {
                return (T)Variables.ActiveScene.Get(variableName);
            }
            return default;
        }
    }
}