using System;
using System.Threading;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Marshals callbacks from background threads (e.g. WebSocket receive loops) onto Unity's main thread.
    /// Uses Unity's own SynchronizationContext, so no helper GameObject is needed.
    /// </summary>
    public static class MainThreadDispatcher {
        private static SynchronizationContext mainThreadContext;
        private static int mainThreadId = -1;

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            mainThreadContext = SynchronizationContext.Current;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeInEditor() {
            Initialize();
        }
#endif

        /// <summary>
        /// Runs the action immediately when called on the main thread, otherwise queues it for the next frame.
        /// </summary>
        public static void Run(Action action) {
            if (action == null) {
                return;
            }
            if (IsMainThread || mainThreadContext == null) {
                action();
                return;
            }
            mainThreadContext.Post(_ => action(), null);
        }

        /// <summary>
        /// Always queues the action for a later main thread update, even when called on the main thread.
        /// </summary>
        public static void Post(Action action) {
            if (action == null) {
                return;
            }
            if (mainThreadContext == null) {
                action();
                return;
            }
            mainThreadContext.Post(_ => action(), null);
        }
    }
}
