using System.IO;
using Unity.WebRTC.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Adapts the imported sample scenes to the project and saves them: replaces StandaloneInputModule with
/// InputSystemUIInputModule when only the Input System is enabled, and the built-in default material
/// (pink in URP and HDRP) with the active render pipeline's default material.
/// Runs automatically after the samples are imported or the project settings change, and from the menu
/// "Tools/WebRTC Samples/Set Up Sample Scenes for This Project".
/// </summary>
[InitializeOnLoad]
static class SampleScenesSetup
{
    // StandaloneInputModule (com.unity.ugui) and the built-in "Default-Material" as referenced in scene files.
    const string StandaloneInputModuleReference = "guid: 4f231c4fb786f3946a6b90b886c48677";
    const string BuiltinDefaultMaterialReference = "fileID: 10303, guid: 0000000000000000f000000000000000";

    static SampleScenesSetup()
    {
        // Wait until the Editor has finished loading and importing.
        EditorApplication.delayCall += () => SetUpScenes(fromMenu: false);
    }

    [MenuItem("Tools/WebRTC Samples/Set Up Sample Scenes for This Project")]
    static void SetUpScenesFromMenu()
    {
        SetUpScenes(fromMenu: true);
    }

    static bool UsesInputSystemOnly
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER && WEBRTC_SAMPLES_INPUT_SYSTEM
            return true;
#else
            return false;
#endif
        }
    }

    static void SetUpScenes(bool fromMenu)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var usesScriptableRenderPipeline = GraphicsSettings.currentRenderPipeline != null;
        if (!UsesInputSystemOnly && !usesScriptableRenderPipeline)
        {
            if (fromMenu)
                Debug.Log("WebRTC samples: this project uses the Input Manager and the built-in render pipeline, the sample scenes need no changes.");
            return;
        }

        var root = SampleScenesBuildMenu.FindSamplesRoot();
        if (root == null)
        {
            if (fromMenu)
                Debug.LogError("WebRTC samples not found. Import them from the Package Manager first.");
            return;
        }

        var changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { root }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // Read the file first, so scenes that are already set up are not opened again.
            var text = File.ReadAllText(path);
            if (!(UsesInputSystemOnly && text.Contains(StandaloneInputModuleReference)) &&
                !(usesScriptableRenderPipeline && text.Contains(BuiltinDefaultMaterialReference)))
                continue;

            var scene = SceneManager.GetSceneByPath(path);
            var wasOpen = scene.isLoaded;
            if (wasOpen && scene.isDirty)
            {
                Debug.LogWarning($"WebRTC samples: {path} has unsaved changes and was not set up. Save it and select Tools/WebRTC Samples/Set Up Sample Scenes for This Project.");
                continue;
            }
            if (!wasOpen)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            if (SampleSceneSetup.Apply(scene))
            {
                EditorSceneManager.SaveScene(scene);
                changed++;
            }

            if (!wasOpen)
                EditorSceneManager.CloseScene(scene, true);
        }

        if (changed > 0 || fromMenu)
            Debug.Log($"WebRTC samples: set up {changed} sample scenes in {root} for this project's input handling and render pipeline.");
    }
}
