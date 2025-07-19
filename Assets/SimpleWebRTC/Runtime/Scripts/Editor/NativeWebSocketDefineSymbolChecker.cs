#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SimpleWebRTC {
    [InitializeOnLoad]
    public static class NativeWebSocketDefineSymbolChecker {
        static NativeWebSocketDefineSymbolChecker() {
            EditorApplication.delayCall += UpdateNativeWebSocketDefine;
        }

        [MenuItem("Tools/Update NativeWebSocket Define Symbol")]
        public static void UpdateNativeWebSocketDefine() {
            string scriptingDefineSymbol = string.Empty;

            var hasMetaNativeWebSocket = HasMetaNativeWebSocket();
            var hasNativeWebSocket = HasNativeWebSocket();

            if (hasMetaNativeWebSocket) {
                scriptingDefineSymbol = "USE_META_NATIVEWEBSOCKET";
            } else if (hasNativeWebSocket) {
                scriptingDefineSymbol = "USE_NATIVEWEBSOCKET";
            }

            var targets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.Android,
                NamedBuildTarget.iOS,
            };

            foreach (var target in targets) {
                try {
                    var defines = PlayerSettings.GetScriptingDefineSymbols(target);
                    var defineList = defines.Split(';')
                                            .Select(s => s.Trim())
                                            .Where(s => !string.IsNullOrEmpty(s))
                                            .ToList();

                    var symbolExists = defineList.Contains(scriptingDefineSymbol);

                    switch (hasMetaNativeWebSocket || hasNativeWebSocket) {
                        case true when !symbolExists:
                            defineList.Add(scriptingDefineSymbol);
                            Debug.Log($"[NativeWebSocketDefineSymbolChecker] Added {scriptingDefineSymbol} for {target}");
                            break;
                        case false when symbolExists:
                            defineList.Remove(scriptingDefineSymbol);
                            Debug.Log($"[NativeWebSocketDefineSymbolChecker] Removed {scriptingDefineSymbol} for {target}");
                            break;
                    }

                    var newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                } catch (Exception ex) {
                    Debug.LogWarning($"[NativeWebSocketDefineSymbolChecker] Could not update define for target {target}: {ex.Message}");
                }
            }
        }

        private static bool HasMetaNativeWebSocket() {
            return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Any(asm => asm.GetName().Name.Contains("Meta.Net.endel.nativewebsocket"));
        }

        private static bool HasNativeWebSocket() {
            return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Any(asm => asm.GetName().Name.Contains("endel.nativewebsocket"));
        }
    }
}
#endif