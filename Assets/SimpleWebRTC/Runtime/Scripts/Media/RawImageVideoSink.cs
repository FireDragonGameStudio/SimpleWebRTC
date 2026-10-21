using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC {
    /// <summary>
    /// Shows received videos in UI RawImages: either one fixed RawImage (latest video wins), or one RawImage per
    /// video spawned under a parent, e.g. a GridLayoutGroup.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Video/RawImage Video Sink")]
    public class RawImageVideoSink : VideoSink {

        [Tooltip("Show the video in this RawImage. Leave empty to spawn one RawImage per video under the parent.")]
        [SerializeField] private RawImage targetImage;
        [SerializeField] private RectTransform spawnParent;
        [Tooltip("Optional template for spawned images. A plain stretched RawImage is created if empty.")]
        [SerializeField] private RawImage imagePrefab;

        private readonly Dictionary<string, RawImage> spawnedImages = new Dictionary<string, RawImage>();
        private string targetImageVideoKey;

        public override void SetVideo(RemoteVideo video) {
            if (targetImage != null) {
                targetImage.texture = video.Texture;
                targetImageVideoKey = video.Key;
                return;
            }
            if (!spawnedImages.TryGetValue(video.Key, out var image) || image == null) {
                image = CreateImage(video);
                spawnedImages[video.Key] = image;
            }
            image.texture = video.Texture;
        }

        public override void RemoveVideo(RemoteVideo video) {
            if (targetImage != null) {
                if (targetImageVideoKey == video.Key) {
                    targetImage.texture = null;
                    targetImageVideoKey = null;
                }
                return;
            }
            if (spawnedImages.TryGetValue(video.Key, out var image)) {
                spawnedImages.Remove(video.Key);
                if (image != null) {
                    Destroy(image.gameObject);
                }
            }
        }

        private RawImage CreateImage(RemoteVideo video) {
            var parent = spawnParent != null ? spawnParent : (RectTransform)transform;
            RawImage image;
            if (imagePrefab != null) {
                image = Instantiate(imagePrefab, parent, false);
            } else {
                image = new GameObject().AddComponent<RawImage>();
                var rect = image.rectTransform;
                rect.SetParent(parent, false);
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero;
            }
            image.name = $"{video.PeerId}-{video.StreamId}-Video";
            return image;
        }
    }
}
