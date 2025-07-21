using UnityEngine;

namespace SimpleWebRTC {
    public class SwitchToNextCameraPosition : MonoBehaviour {

        [SerializeField] private WebRTCConnection webRTCConnection;
        [SerializeField] private string cameraSwitchKeyword = "switch";
        [SerializeField] private bool manualPositionSwitch = false;
        [SerializeField] private bool webRTCPositionSwitch = false;
        [SerializeField] private Transform[] cameraParentObjects;
        [Header("Sending Camera Position to Sender")]
        [SerializeField] private bool enableSendingPosition = false;
        [SerializeField] private float sendingIntervalInSeconds = 0.1f;

        private int cameraPositionCounter = 0;
        private float sendingIntervalCounter = 0;

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
            if (webRTCPositionSwitch && webRTCConnection.IsWebRTCActive) {
                webRTCPositionSwitch = false;
                webRTCConnection.SendDataChannelMessage(cameraSwitchKeyword);
            }
            if (enableSendingPosition && webRTCConnection.IsWebRTCActive && webRTCConnection.IsImmersiveSetupActive && webRTCConnection.IsReceiver && webRTCConnection.ExperimentalSupportFor6DOF) {
                sendingIntervalCounter += Time.deltaTime;
                if (sendingIntervalCounter >= sendingIntervalInSeconds) {
                    sendingIntervalCounter = 0;
                    webRTCConnection.SendDataChannelMessage($"{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.x}||||{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.y}||||{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.z}");
                }
            }
        }

        public void OnMessageReceived(string message) {
            if (webRTCConnection.IsImmersiveSetupActive && webRTCConnection.IsSender) {
                string[] trylocalPosition = message.Split("||||");
                bool isPositionMessage = trylocalPosition.Length == 3;
                if (webRTCConnection.ExperimentalSupportFor6DOF && isPositionMessage) {
                    webRTCConnection.VideoStreamingCamera.transform.localPosition = new Vector3(float.Parse(trylocalPosition[0]), float.Parse(trylocalPosition[1]), float.Parse(trylocalPosition[2]));
                } else if (message.ToLower().Equals(cameraSwitchKeyword.ToLower()) && cameraParentObjects.Length > 0) {
                    cameraPositionCounter = (cameraPositionCounter + 1) % cameraParentObjects.Length;
                    webRTCConnection.VideoStreamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
                }
            }
        }
    }
}