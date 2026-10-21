using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Shows the received video on a mesh, e.g. a quad in world space. Shows the first active video.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Renderer Video Sink")]
    public class RendererVideoSink : VideoSink {

        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Texture property to set. Leave empty to use the material's main texture (_BaseMap or _MainTex).")]
        [SerializeField] private string textureProperty = "";

        private RemoteVideo currentVideo;
        private MaterialPropertyBlock propertyBlock;

        private void Reset() {
            targetRenderer = GetComponent<Renderer>();
        }

        public override void SetVideo(RemoteVideo video) {
            if (currentVideo != null && currentVideo.Key != video.Key) {
                return;
            }
            currentVideo = video;
            ApplyTexture(video.Texture);
        }

        public override void RemoveVideo(RemoteVideo video) {
            if (currentVideo == null || currentVideo.Key != video.Key) {
                return;
            }
            currentVideo = null;
            ApplyTexture(null);
        }

        private void ApplyTexture(Texture texture) {
            if (targetRenderer == null) {
                return;
            }
            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            if (texture != null) {
                propertyBlock.SetTexture(ResolvePropertyName(), texture);
            } else {
                propertyBlock.Clear();
            }
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private string ResolvePropertyName() {
            if (!string.IsNullOrEmpty(textureProperty)) {
                return textureProperty;
            }
            var material = targetRenderer.sharedMaterial;
            return material != null && material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        }
    }
}
