using Unity.WebRTC;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Runs exactly one <see cref="WebRTC.Update"/> coroutine for the whole application, no matter how many
    /// peer managers exist. Video tracks need this coroutine to copy textures each frame.
    /// </summary>
    public sealed class WebRTCUpdateRunner : MonoBehaviour {
        private static WebRTCUpdateRunner instance;
        private static int users;

        private Coroutine updateRoutine;

        public static void Acquire() {
            users++;
            if (instance == null) {
                var go = new GameObject("[SimpleWebRTC] WebRTC.Update") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<WebRTCUpdateRunner>();
            }
            if (instance.updateRoutine == null) {
                instance.updateRoutine = instance.StartCoroutine(WebRTC.Update());
            }
        }

        public static void Release() {
            users = Mathf.Max(0, users - 1);
            if (users == 0 && instance != null) {
                Destroy(instance.gameObject);
                instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            instance = null;
            users = 0;
        }
    }
}
