using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleWebRTC {
    /// <summary>
    /// Streams an immersive 360° video: renders a camera into a cubemap and converts it to an equirectangular
    /// texture, monoscopic or stereo (over-under). Rendering 360° video is expensive.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Cubemap (360°) Video Source")]
    public class CubemapVideoSource : VideoSource {

        [SerializeField] private Camera sourceCamera;
        [Tooltip("Render both eyes, stacked over-under in the equirect texture.")]
        [SerializeField] private bool renderStereo = false;
        [SerializeField] private float stereoSeparation = 0.064f;
        [Tooltip("Size of one cubemap face. The equirect texture has this width.")]
        [SerializeField] private int eyeResolution = 1024;
        [SerializeField] private RenderTextureDepth depth = RenderTextureDepth.Depth24;
        [Tooltip("Render one cubemap face per frame instead of all six, trading latency for performance.")]
        [SerializeField] private bool oneFacePerFrame = false;
        [Tooltip("Activates the camera GameObject only while streaming.")]
        [SerializeField] private bool activateCameraWhileStreaming = true;

        private RenderTexture cubemapLeftEye;
        private RenderTexture cubemapRightEye;
        private RenderTexture equirect;
        private bool isRendering;

        public Camera SourceCamera => sourceCamera;
        public RenderTexture EquirectTexture => equirect;

        public override IEnumerator CreateTrack(Action<VideoStreamTrack> onCreated) {
            if (sourceCamera == null) {
                SimpleWebRTCLogger.LogError($"{name}: no camera assigned to {nameof(CubemapVideoSource)}.");
                onCreated(null);
                yield break;
            }
            if (activateCameraWhileStreaming) {
                sourceCamera.gameObject.SetActive(true);
            }
            CreateTextures();
            isRendering = true;
            onCreated(new VideoStreamTrack(equirect));
        }

        public override void ReleaseTrack(VideoStreamTrack track) {
            base.ReleaseTrack(track);
            isRendering = false;
            ReleaseTextures();
            if (sourceCamera != null && activateCameraWhileStreaming) {
                sourceCamera.gameObject.SetActive(false);
            }
        }

        private void LateUpdate() {
            if (!isRendering || sourceCamera == null) {
                return;
            }
            var faceMask = oneFacePerFrame ? 1 << (Time.frameCount % 6) : 63;

            if (renderStereo) {
                sourceCamera.stereoSeparation = stereoSeparation;
                sourceCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);
                sourceCamera.RenderToCubemap(cubemapRightEye, faceMask, Camera.MonoOrStereoscopicEye.Right);
                cubemapLeftEye.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
                cubemapRightEye.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);
            } else {
                sourceCamera.RenderToCubemap(cubemapLeftEye, faceMask, Camera.MonoOrStereoscopicEye.Left);
                cubemapLeftEye.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Mono);
            }
        }

        private void CreateTextures() {
            ReleaseTextures();
            cubemapLeftEye = CreateCubemap();
            if (renderStereo) {
                cubemapRightEye = CreateCubemap();
            }
            // stereo is stacked over-under, so the equirect needs twice the height
            equirect = new RenderTexture(eyeResolution, renderStereo ? eyeResolution * 2 : eyeResolution, (int)depth, RenderTextureFormat.BGRA32) {
                hideFlags = HideFlags.HideAndDontSave
            };
            equirect.Create();
        }

        private RenderTexture CreateCubemap() {
            var cubemap = new RenderTexture(eyeResolution, eyeResolution, (int)depth, RenderTextureFormat.BGRA32) {
                dimension = TextureDimension.Cube,
                hideFlags = HideFlags.HideAndDontSave
            };
            cubemap.Create();
            return cubemap;
        }

        private void ReleaseTextures() {
            DestroyTexture(ref cubemapLeftEye);
            DestroyTexture(ref cubemapRightEye);
            DestroyTexture(ref equirect);
        }

        private static void DestroyTexture(ref RenderTexture texture) {
            if (texture == null) {
                return;
            }
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        private void OnDestroy() {
            ReleaseTextures();
        }
    }
}
