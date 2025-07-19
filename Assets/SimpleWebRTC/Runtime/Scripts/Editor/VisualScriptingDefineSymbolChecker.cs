#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SimpleWebRTC {
    [InitializeOnLoad]
    public static class VisualScriptingDefineSymbolChecker {
        static VisualScriptingDefineSymbolChecker() {
            UpdateVisualScriptingDefine();
        }

        [MenuItem("Tools/Update VisualScripting Define Symbol")]
        public static void UpdateVisualScriptingDefine() {
            var hasVisualScripting = HasVisualScripting();

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

                    var symbolExists = defineList.Contains("VISUAL_SCRIPTING_INSTALLED");

                    switch (hasVisualScripting) {
                        case true when !symbolExists:
                            defineList.Add("VISUAL_SCRIPTING_INSTALLED");
                            Debug.Log($"[VisualScriptingDefineSymbolChecker] Added VISUAL_SCRIPTING_INSTALLED for {target}");
                            break;
                        case false when symbolExists:
                            defineList.Remove("VISUAL_SCRIPTING_INSTALLED");
                            Debug.Log($"[VisualScriptingDefineSymbolChecker] Removed VISUAL_SCRIPTING_INSTALLED for {target}");
                            break;
                    }

                    var newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                } catch (Exception ex) {
                    Debug.LogWarning($"[VisualScriptingDefineSymbolChecker] Could not update define for target {target}: {ex.Message}");
                }
            }
        }

        private static bool HasVisualScripting() {
            return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Any(asm => asm.GetName().Name.Contains("Unity.VisualScripting"));
        }
    }
}
#endif