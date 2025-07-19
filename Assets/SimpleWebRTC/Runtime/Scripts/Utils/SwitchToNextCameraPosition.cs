using SimpleWebRTC;
using UnityEngine;

public class SwitchToNextCameraPosition : MonoBehaviour {

    [SerializeField] private WebRTCConnection webRTCConnection;
    [SerializeField] private string cameraSwitchKeyword = "switch";
    [SerializeField] private bool manualPositionSwitch = false;
    [SerializeField] private bool webRTCPositionSwitch = false;
    [SerializeField] private Transform[] cameraParentObjects;

    private int cameraPositionCounter = 0;

    private void Start() {
        if (webRTCConnection.IsImmersiveSetupActive && cameraParentObjects.Length > 0) {
            webRTCConnection.VideoStreamingCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            webRTCConnection.VideoStreamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
        }
    }

    private void Update() {
        if (manualPositionSwitch) {
            manualPositionSwitch = false;
            OnMessageReceived(cameraSwitchKeyword);
        }
        if (webRTCPositionSwitch) {
            webRTCPositionSwitch = false;
            webRTCConnection.SendDataChannelMessage(cameraSwitchKeyword);
        }
    }

    public void OnMessageReceived(string message) {
        if (webRTCConnection.IsImmersiveSetupActive && webRTCConnection.IsSender && message.ToLower().Equals(cameraSwitchKeyword.ToLower()) && cameraParentObjects.Length > 0) {
            cameraPositionCounter = (cameraPositionCounter + 1) % cameraParentObjects.Length;
            webRTCConnection.VideoStreamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
        }
    }
}
