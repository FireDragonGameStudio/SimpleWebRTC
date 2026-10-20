using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the sample scenes to the scene list of the build profile, with the Menu scene first,
/// so a player build of the samples can switch between them. Play mode in the Editor does not need this.
/// </summary>
static class SampleScenesBuildMenu
{
    const string MenuScene = "Menu";

    [MenuItem("Tools/WebRTC Samples/Add Sample Scenes to Build Profile")]
    static void AddSampleScenes()
    {
        var root = FindSamplesRoot();
        if (root == null)
        {
            Debug.LogError("WebRTC samples not found. Import them from the Package Manager first.");
            return;
        }

        var samplePaths = AssetDatabase.FindAssets("t:Scene", new[] { root })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => Path.GetFileNameWithoutExtension(path) == MenuScene ? 0 : 1)
            .ThenBy(path => path)
            .ToList();

        var scenes = EditorBuildSettings.scenes.ToList();
        var added = 0;
        foreach (var path in samplePaths)
        {
            if (scenes.Any(scene => scene.path == path))
                continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            added++;
        }

        // The player starts with the first scene in the list, so put the samples' Menu first.
        var menuPath = samplePaths.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == MenuScene);
        var menu = scenes.FirstOrDefault(scene => scene.path == menuPath);
        if (menu != null)
        {
            scenes.Remove(menu);
            scenes.Insert(0, menu);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"Added {added} WebRTC sample scenes to the build profile ({root}).");
    }

    internal static string FindSamplesRoot()
    {
        return AssetDatabase.FindAssets("Unity.WebRTC.Samples t:AssemblyDefinitionAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileName(path) == "Unity.WebRTC.Samples.asmdef")
            .Select(path => Path.GetDirectoryName(path)?.Replace('\\', '/'))
            .FirstOrDefault();
    }
}
