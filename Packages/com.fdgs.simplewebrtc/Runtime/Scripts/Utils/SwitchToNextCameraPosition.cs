using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

namespace SimpleWebRTC {
    /// <summary>
    /// Sample for the immersive scenes: the receiver sends commands over a data channel to move the 360° camera
    /// of the sender, either to the next predefined position or (experimental 6DOF) to the receiver's head position.
    /// </summary>
    public class SwitchToNextCameraPosition : MonoBehaviour {

        [SerializeField] private WebRTCPeerManager peerManager;
        [SerializeField] private DataChannel dataChannel;
        [Tooltip("Sender only: the video sender that streams the 360° camera.")]
        [SerializeField] private VideoSender videoSender;
        [Tooltip("Sender only: the camera that renders the 360° video.")]
        [SerializeField] private Camera streamingCamera;
        [SerializeField] private bool connectOnStart = true;

        [Header("Camera Switch")]
        [SerializeField] private string cameraSwitchKeyword = "switch";
        [SerializeField] private Transform[] cameraParentObjects;
        [Tooltip("Receiver: set to send the switch command.")]
        [SerializeField, FormerlySerializedAs("webRTCPositionSwitch")] private bool sendCameraSwitch = false;
        [Tooltip("Sender: set to start or stop the video stream.")]
        [SerializeField, FormerlySerializedAs("webRTCStartStopVideoStream")] private bool toggleVideoStream = false;

        [Header("Experimental 6DOF (receiver)")]
        [SerializeField] private bool syncCameraPosition = false;
        [SerializeField] private Transform spectatorCamera;
        [SerializeField] private float sendingIntervalInSeconds = 0.1f;

        private const string positionSeparator = "||||";

        private int cameraPositionCounter = 0;
        private float sendingIntervalCounter = 0;
        private readonly NumberFormatInfo numberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." };

        private bool IsSender => videoSender != null && streamingCamera != null;
        private bool IsConnected => peerManager != null && peerManager.ConnectedPeerCount > 0;

        private void Start() {
            if (connectOnStart) {
                peerManager.Connect();
            }
            if (IsSender && cameraParentObjects.Length > 0) {
                streamingCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                streamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
            }
        }

        private void Update() {
            if (toggleVideoStream && IsSender) {
                toggleVideoStream = false;
                videoSender.ToggleStreaming();
            }

            if (sendCameraSwitch && IsConnected) {
                sendCameraSwitch = false;
                SendCameraSwitch();
            }

            if (syncCameraPosition && spectatorCamera != null && IsConnected) {
                sendingIntervalCounter += Time.deltaTime;
                if (sendingIntervalCounter >= sendingIntervalInSeconds) {
                    sendingIntervalCounter = 0;
                    var position = spectatorCamera.localPosition;
                    dataChannel.Send(string.Join(positionSeparator,
                        position.x.ToString(numberFormat), position.y.ToString(numberFormat), position.z.ToString(numberFormat)));
                }
            }
        }

        public void SendCameraSwitch() {
            dataChannel.Send(cameraSwitchKeyword);
        }

        /// <summary>
        /// Wire this to <see cref="DataChannel.MessageReceived"/> on the sender.
        /// </summary>
        public void OnMessageReceived(string message) {
            if (!IsSender) {
                return;
            }
            var parts = message.Split(positionSeparator);
            if (parts.Length == 3
                && float.TryParse(parts[0], NumberStyles.Float, numberFormat, out var x)
                && float.TryParse(parts[1], NumberStyles.Float, numberFormat, out var y)
                && float.TryParse(parts[2], NumberStyles.Float, numberFormat, out var z)) {
                streamingCamera.transform.localPosition = new Vector3(x, y, z);
            } else if (string.Equals(message, cameraSwitchKeyword, System.StringComparison.OrdinalIgnoreCase) && cameraParentObjects.Length > 0) {
                cameraPositionCounter = (cameraPositionCounter + 1) % cameraParentObjects.Length;
                streamingCamera.transform.SetParent(cameraParentObjects[cameraPositionCounter], false);
            }
        }
    }
}
