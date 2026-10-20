using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Streams any texture, e.g. a RenderTexture you draw into or the output of a VideoPlayer.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/Texture Video Source")]
    public class TextureVideoSource : VideoSource {

        [SerializeField] private Texture sourceTexture;

        public Texture SourceTexture {
            get => sourceTexture;
            set => sourceTexture = value;
        }

        public override IEnumerator CreateTrack(Action<VideoStreamTrack> onCreated) {
            if (sourceTexture == null) {
                SimpleWebRTCLogger.LogError($"{name}: no texture assigned to {nameof(TextureVideoSource)}.");
                onCreated(null);
                yield break;
            }
            onCreated(new VideoStreamTrack(sourceTexture));
        }
    }
}
