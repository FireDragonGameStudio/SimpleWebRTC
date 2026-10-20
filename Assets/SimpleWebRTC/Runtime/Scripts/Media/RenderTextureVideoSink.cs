using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Copies the received video into a RenderTexture every frame, e.g. for a 360° skybox material or any
    /// material that already uses the RenderTexture. Shows the first active video.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/RenderTexture Video Sink")]
    public class RenderTextureVideoSink : VideoSink {

        [SerializeField] private RenderTexture targetTexture;
        [Tooltip("Clear the target texture when the video stops.")]
        [SerializeField] private bool clearOnStop = true;

        private RemoteVideo currentVideo;

        public RenderTexture TargetTexture {
            get => targetTexture;
            set => targetTexture = value;
        }

        public override void SetVideo(RemoteVideo video) {
            if (currentVideo == null || currentVideo.Key == video.Key) {
                currentVideo = video;
            }
        }

        public override void RemoveVideo(RemoteVideo video) {
            if (currentVideo == null || currentVideo.Key != video.Key) {
                return;
            }
            currentVideo = null;
            if (clearOnStop && targetTexture != null) {
                var active = RenderTexture.active;
                RenderTexture.active = targetTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = active;
            }
        }

        private void Update() {
            if (currentVideo?.Texture != null && targetTexture != null) {
                Graphics.Blit(currentVideo.Texture, targetTexture);
            }
        }
    }
}
