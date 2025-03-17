using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportUPMPackage {
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
}
