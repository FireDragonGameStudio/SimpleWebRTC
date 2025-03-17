using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ExportUPMPackage {
#if UNITY_EDITOR
    [MenuItem("Tools/Export UPM Package")]
    public static void ExportPackage() {
        string packagePath = "Assets/SimpleWebRTC";  // Change to match your package folder
        string exportPath = Path.Combine(Application.dataPath, "../SimpleWebRTCPackage");

        if (!Directory.Exists(packagePath)) {
            Debug.LogError("Package path not found: " + packagePath);
            return;
        }

        Debug.Log("Exporting package...");
        UnityEditor.PackageManager.Client.Pack(packagePath, exportPath);
        Debug.Log("Exporting package finished! " + exportPath);
    }
#endif
}
