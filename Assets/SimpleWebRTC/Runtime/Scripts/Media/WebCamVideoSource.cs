using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Streams a webcam. Remember to request camera permission on mobile platforms before streaming.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/WebCam Video Source")]
    public class WebCamVideoSource : VideoSource {

        [Tooltip("Leave empty to use the default device.")]
        [SerializeField] private string deviceName = "";
        [SerializeField] private Vector2Int resolution = new Vector2Int(1280, 720);
        [SerializeField] private int frameRate = 30;
        [SerializeField] private float startTimeoutSeconds = 5f;

        private WebCamTexture webCamTexture;

        public WebCamTexture WebCamTexture => webCamTexture;

        public override IEnumerator CreateTrack(Action<VideoStreamTrack> onCreated) {
            if (WebCamTexture.devices.Length == 0) {
                SimpleWebRTCLogger.LogError($"{name}: no webcam found.");
                onCreated(null);
                yield break;
            }

            webCamTexture = string.IsNullOrEmpty(deviceName)
                ? new WebCamTexture(resolution.x, resolution.y, frameRate)
                : new WebCamTexture(deviceName, resolution.x, resolution.y, frameRate);
            webCamTexture.Play();

            var timeout = Time.realtimeSinceStartup + startTimeoutSeconds;
            yield return new WaitUntil(() => webCamTexture.didUpdateThisFrame || Time.realtimeSinceStartup > timeout);

            if (!webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame) {
                SimpleWebRTCLogger.LogError($"{name}: webcam '{webCamTexture.deviceName}' did not start.");
                StopWebCam();
                onCreated(null);
                yield break;
            }
            onCreated(new VideoStreamTrack(webCamTexture));
        }

        public override void ReleaseTrack(VideoStreamTrack track) {
            base.ReleaseTrack(track);
            StopWebCam();
        }

        private void StopWebCam() {
            if (webCamTexture == null) {
                return;
            }
            webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }

        private void OnDestroy() {
            StopWebCam();
        }
    }
}
