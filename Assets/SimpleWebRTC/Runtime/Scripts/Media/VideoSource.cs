using System;
using System.Collections;
using Unity.WebRTC;

namespace SimpleWebRTC {
    /// <summary>
    /// Provides the video track a <see cref="VideoSender"/> streams. Derive from it for custom sources.
    /// </summary>
    public abstract class VideoSource : SimpleWebRTCBehaviour {

        /// <summary>
        /// Creates the track. Implemented as a coroutine because some sources (e.g. webcams) need time to start.
        /// Invoke <paramref name="onCreated"/> with the track, or with null if the source could not start.
        /// </summary>
        public abstract IEnumerator CreateTrack(Action<VideoStreamTrack> onCreated);

        /// <summary>
        /// Called when streaming stops. Stop and dispose the track and release everything created for it.
        /// </summary>
        public virtual void ReleaseTrack(VideoStreamTrack track) {
            if (track == null) {
                return;
            }
            track.Stop();
            track.Dispose();
        }
    }
}
