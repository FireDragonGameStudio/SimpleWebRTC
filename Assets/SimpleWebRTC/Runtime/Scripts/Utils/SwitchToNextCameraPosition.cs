using System.Globalization;
using UnityEngine;

namespace SimpleWebRTC {
    public class SwitchToNextCameraPosition : MonoBehaviour {

        [SerializeField] private WebRTCConnection webRTCConnection;
        [SerializeField] private string cameraSwitchKeyword = "switch";
        [SerializeField] private Transform[] cameraParentObjects;
        [Header("Trigger video streaming on sender")]
        [SerializeField] private bool webRTCStartStopVideoStream = false;
        [Header("Trigger camera switch from receiver")]
        [SerializeField] private bool webRTCPositionSwitch = false;
        [Header("Sending Camera Position to Sender")]
        [SerializeField] private bool syncCameraPosition = false;
        [SerializeField] private float sendingIntervalInSeconds = 0.1f;

        private int cameraPositionCounter = 0;
        private float sendingIntervalCounter = 0;

        // make sure the float decimal separator is converted correctly
        private NumberFormatInfo numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };

        private void Start() {
            webRTCConnection.Connect();

            if (webRTCConnection.IsImmersiveSetupActive && cameraParentObjects.Length > 0) {
                webRTCConnection.VideoStreamingCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                webRTCConnection.VideoStreamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
            }
        }

        private void Update() {
            // example: use the input system keyboard for starting/stopping the video stream
            //if (Keyboard.current.vKey.wasPressedThisFrame) {
            //    webRTCConnection.StartVideoTransmission();
            //}
            //if (Keyboard.current.bKey.wasPressedThisFrame) {
            //    webRTCConnection.StopVideoTransmission();
            //}

            // example: use the OVR input for sending the camera switch
            //if (OVRInput.Get(OVRInput.Button.Start)) {
            //    webRTCConnection.SendDataChannelMessage(cameraSwitchKeyword);
            //}

            // use the boolean flag for starting/stopping the camera stream
            if (webRTCStartStopVideoStream && webRTCConnection.IsSender && webRTCConnection.IsWebRTCActive) {
                webRTCStartStopVideoStream = false;
                if (webRTCConnection.IsVideoTransmissionActive) {
                    webRTCConnection.StopVideoTransmission();
                } else {
                    webRTCConnection.StartVideoTransmission();
                }
            }

            // use the boolean flag for sending the camera switch
            if (webRTCPositionSwitch && webRTCConnection.IsWebRTCActive && webRTCConnection.IsReceiver) {
                webRTCPositionSwitch = false;
                webRTCConnection.SendDataChannelMessage(cameraSwitchKeyword);
            }
            if (syncCameraPosition && webRTCConnection.IsWebRTCActive && webRTCConnection.IsImmersiveSetupActive && webRTCConnection.IsReceiver && webRTCConnection.ExperimentalSupportFor6DOF) {
                sendingIntervalCounter += Time.deltaTime;
                if (sendingIntervalCounter >= sendingIntervalInSeconds) {
                    sendingIntervalCounter = 0;
                    webRTCConnection.SendDataChannelMessage($"{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.x}||||{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.y}||||{webRTCConnection.ExperimentalSpectatorCam6DOF.localPosition.z}");
                }
            }
        }

        private void OnDestroy() {
            webRTCConnection.Disconnect();
        }

        public void OnMessageReceived(string message) {
            if (webRTCConnection.IsImmersiveSetupActive && webRTCConnection.IsSender) {
                string[] trylocalPosition = message.Split("||||");
                bool isPositionMessage = trylocalPosition.Length == 3;
                if (webRTCConnection.ExperimentalSupportFor6DOF && isPositionMessage) {
                    Debug.Log($"x = {trylocalPosition[0]} = {float.Parse(trylocalPosition[0], numberFormatInfo)}, y = {trylocalPosition[1]} = {float.Parse(trylocalPosition[1], numberFormatInfo)}, z = {trylocalPosition[2]} = {float.Parse(trylocalPosition[2], numberFormatInfo)}");

                    webRTCConnection.VideoStreamingCamera.transform.localPosition = new Vector3(
                        float.Parse(trylocalPosition[0], numberFormatInfo),
                        float.Parse(trylocalPosition[1], numberFormatInfo),
                        float.Parse(trylocalPosition[2], numberFormatInfo));

                } else if (message.ToLower().Equals(cameraSwitchKeyword.ToLower()) && cameraParentObjects.Length > 0) {
                    cameraPositionCounter = (cameraPositionCounter + 1) % cameraParentObjects.Length;
                    webRTCConnection.VideoStreamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
                }
            }
        }
    }
}