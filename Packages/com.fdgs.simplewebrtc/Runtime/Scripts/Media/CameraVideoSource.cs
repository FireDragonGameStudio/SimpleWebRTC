using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Streams what a camera renders.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Camera Video Source")]
    public class CameraVideoSource : VideoSource {

        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Vector2Int resolution = new Vector2Int(1280, 720);
        [SerializeField] private RenderTextureDepth depth = RenderTextureDepth.Depth24;
        [Tooltip("Activates the camera GameObject only while streaming, so it does not render otherwise.")]
        [SerializeField] private bool activateCameraWhileStreaming = true;

        public Camera SourceCamera {
            get => sourceCamera;
            set => sourceCamera = value;
        }

        public Vector2Int Resolution {
            get => resolution;
            set => resolution = value;
        }

        private void Reset() {
            sourceCamera = GetComponent<Camera>();
        }

        public override IEnumerator CreateTrack(Action<VideoStreamTrack> onCreated) {
            if (sourceCamera == null) {
                SimpleWebRTCLogger.LogError($"{name}: no camera assigned to {nameof(CameraVideoSource)}.");
                onCreated(null);
                yield break;
            }
            if (activateCameraWhileStreaming) {
                sourceCamera.gameObject.SetActive(true);
            }
            onCreated(sourceCamera.CaptureStreamTrack(resolution.x, resolution.y, depth));
        }

        public override void ReleaseTrack(VideoStreamTrack track) {
            base.ReleaseTrack(track);
            if (sourceCamera == null) {
                return;
            }
            var renderTexture = sourceCamera.targetTexture;
            sourceCamera.targetTexture = null;
            if (renderTexture != null) {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (activateCameraWhileStreaming) {
                sourceCamera.gameObject.SetActive(false);
            }
        }
    }
}
