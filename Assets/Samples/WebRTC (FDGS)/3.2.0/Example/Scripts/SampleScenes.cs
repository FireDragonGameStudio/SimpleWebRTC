using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.WebRTC.Samples
{
    /// <summary>
    /// Loads the sample scenes. In the Editor the scenes are loaded by path, so the samples work in Play mode
    /// without adding them to the scene list of the build profile. A player can only load scenes from that list:
    /// use the menu item "Tools/WebRTC Samples/Add Sample Scenes to Build Profile" before building.
    /// </summary>
    internal static class SampleScenes
    {
        public const string MenuScene = "Menu";

        public static bool CanLoad(string sceneName)
        {
#if UNITY_EDITOR
            if (FindScenePath(sceneName) != null)
                return true;
#endif
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static void Load(string sceneName)
        {
#if UNITY_EDITOR
            var path = FindScenePath(sceneName);
            if (path != null)
            {
                EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns the path of a sample scene, which lives in "{samples root}/{name}/{name}.unity".
        /// </summary>
        internal static string FindScenePath(string sceneName)
        {
            var root = FindSamplesRoot();
            if (root == null)
                return null;
            var path = $"{root}/{sceneName}/{sceneName}.unity";
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null ? path : null;
        }

        /// <summary>
        /// Returns the folder that contains the samples' assembly definition. When the samples were imported
        /// more than once, prefers the copy that contains the active scene.
        /// </summary>
        internal static string FindSamplesRoot()
        {
            string fallback = null;
            var activeScene = SceneManager.GetActiveScene().path;
            foreach (var guid in AssetDatabase.FindAssets("Unity.WebRTC.Samples t:AssemblyDefinitionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) != "Unity.WebRTC.Samples.asmdef")
                    continue;
                var root = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(activeScene) && activeScene.StartsWith(root + "/"))
                    return root;
                fallback ??= root;
            }
            return fallback;
        }
#endif
    }
}
