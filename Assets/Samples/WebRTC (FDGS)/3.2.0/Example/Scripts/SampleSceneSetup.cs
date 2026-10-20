using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && WEBRTC_SAMPLES_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Unity.WebRTC.Samples
{
    /// <summary>
    /// Adapts the sample scenes to the project, so the samples work in projects that use the
    /// Input System package or a scriptable render pipeline (URP, HDRP):
    /// <list type="bullet">
    /// <item>Replaces StandaloneInputModule with InputSystemUIInputModule when only the Input System is enabled.</item>
    /// <item>Replaces the built-in default material, which renders pink in a scriptable render pipeline,
    /// with the default material of the active render pipeline.</item>
    /// </list>
    /// The Editor applies this to the imported scene files (see SampleScenesSetup in the Editor folder).
    /// At runtime it is applied again to every sample scene that is loaded, as a fallback.
    /// </summary>
    public static class SampleSceneSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // With "Enter Play Mode Options" and scene reload disabled, sceneLoaded is not raised for
        // the scenes that are already open when entering Play mode.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyToLoadedScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                Apply(SceneManager.GetSceneAt(i));
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply(scene);
        }

        /// <summary>
        /// Adapts a sample scene to the project. Returns true if it changed the scene.
        /// Scenes that are not part of the samples are left untouched.
        /// </summary>
        public static bool Apply(Scene scene)
        {
            if (!scene.isLoaded || !IsSampleScene(scene))
                return false;

            var changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                changed |= ReplaceInputModules(root);
                changed |= ReplaceBuiltinMaterials(root);
            }
            return changed;
        }

        // Only touch the scenes of these samples, never other scenes of the project.
        static bool IsSampleScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<BackButton>(true) != null ||
                    root.GetComponentInChildren<SceneSelectUI>(true) != null)
                    return true;
            }
            return false;
        }

        static bool ReplaceInputModules(GameObject root)
        {
            var changed = false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER && WEBRTC_SAMPLES_INPUT_SYSTEM
            foreach (var module in root.GetComponentsInChildren<StandaloneInputModule>(true))
            {
                var gameObject = module.gameObject;
                Object.DestroyImmediate(module);
                if (gameObject.GetComponent<InputSystemUIInputModule>() == null)
                    gameObject.AddComponent<InputSystemUIInputModule>();
                changed = true;
            }
#endif
            return changed;
        }

        static bool ReplaceBuiltinMaterials(GameObject root)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || pipeline.defaultMaterial == null)
                return false;

            var anyChanged = false;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (IsBuiltinMaterial(materials[i]))
                    {
                        materials[i] = pipeline.defaultMaterial;
                        changed = true;
                    }
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    anyChanged = true;
                }
            }
            return anyChanged;
        }

        // The scenes use the built-in "Default-Material" with the built-in render pipeline's "Standard" shader.
        static bool IsBuiltinMaterial(Material material)
        {
            return material == null || material.shader == null || !material.shader.isSupported ||
                material.shader.name == "Standard";
        }
    }
}
