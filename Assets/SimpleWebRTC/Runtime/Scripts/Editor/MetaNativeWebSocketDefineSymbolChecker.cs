#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SimpleWebRTC {
    [InitializeOnLoad]
    public static class MetaNativeWebSocketDefineSymbolChecker {
        static MetaNativeWebSocketDefineSymbolChecker() {
            EditorApplication.delayCall += UpdateMetaNativeWebSocketDefine;
        }

        [MenuItem("Tools/Update Meta NativeWebSocket Define Symbol")]
        public static void UpdateMetaNativeWebSocketDefine() {
            var hasVisualScripting = HasMetaNativeWebSocket();

            var targets = new[]
            {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
        };

            foreach (var target in targets) {
                try {
                    var defines = PlayerSettings.GetScriptingDefineSymbols(target);
                    var defineList = defines.Split(';')
                                            .Select(s => s.Trim())
                                            .Where(s => !string.IsNullOrEmpty(s))
                                            .ToList();

                    var symbolExists = defineList.Contains("USE_META_NATIVEWEBSOCKET");

                    switch (hasVisualScripting) {
                        case true when !symbolExists:
                            defineList.Add("USE_META_NATIVEWEBSOCKET");
                            Debug.Log($"[MetaNativeWebSocketDefineSymbolChecker] Added USE_META_NATIVEWEBSOCKET for {target}");
                            break;
                        case false when symbolExists:
                            defineList.Remove("USE_META_NATIVEWEBSOCKET");
                            Debug.Log($"[MetaNativeWebSocketDefineSymbolChecker] Removed USE_META_NATIVEWEBSOCKET for {target}");
                            break;
                    }

                    var newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                } catch (Exception ex) {
                    Debug.LogWarning($"[MetaNativeWebSocketDefineSymbolChecker] Could not update define for target {target}: {ex.Message}");
                }
            }
        }

        private static bool HasMetaNativeWebSocket() {
            return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Any(asm => asm.GetName().Name.Contains("Meta.Net.NativeWebSocket"));
        }
    }
}
#endif