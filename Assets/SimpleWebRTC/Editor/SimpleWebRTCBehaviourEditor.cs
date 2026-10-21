using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SimpleWebRTC.Editor {
    /// <summary>
    /// Default inspector plus Play mode buttons for all methods marked with <see cref="InspectorButtonAttribute"/>.
    /// </summary>
    [CustomEditor(typeof(SimpleWebRTCBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class SimpleWebRTCBehaviourEditor : UnityEditor.Editor {

        private List<(MethodInfo method, string label)> buttons;

        private void OnEnable() {
            buttons = new List<(MethodInfo, string)>();
            for (var type = target.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType) {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    var attribute = method.GetCustomAttribute<InspectorButtonAttribute>();
                    if (attribute != null && method.GetParameters().Length == 0 && buttons.All(b => b.method.Name != method.Name)) {
                        buttons.Add((method, attribute.Label ?? ObjectNames.NicifyVariableName(method.Name)));
                    }
                }
            }
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            DrawRuntimeInfo();

            if (buttons.Count == 0) {
                return;
            }
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                EditorGUILayout.LabelField(Application.isPlaying ? "Actions" : "Actions (Play mode only)", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope()) {
                    foreach (var (method, label) in buttons) {
                        if (GUILayout.Button(label)) {
                            foreach (var t in targets) {
                                method.Invoke(t, null);
                            }
                        }
                    }
                }
            }
        }

        private void DrawRuntimeInfo() {
            if (!Application.isPlaying || targets.Length > 1) {
                return;
            }
            string info = null;
            switch (target) {
                case WebRTCPeerManager manager:
                    info = $"Peer id: {manager.LocalPeerId}\nSignaling: {(manager.Signaling != null ? manager.Signaling.State.ToString() : "none")}\n" +
                        $"Peers: {string.Join(", ", manager.Sessions.Select(s => $"{s.RemotePeerId} ({s.ConnectionState})"))}";
                    break;
                case SignalingTransport transport:
                    info = $"State: {transport.State}";
                    break;
                case DataChannel channel:
                    info = $"Open channels: {channel.OpenChannelCount}";
                    break;
                case VideoSender sender:
                    info = $"Streaming: {sender.IsStreaming}";
                    break;
                case AudioSender sender:
                    info = $"Streaming: {sender.IsStreaming}";
                    break;
                case WebRTCStatsMonitor monitor:
                    info = string.IsNullOrEmpty(monitor.Summary) ? "Waiting for the first update..." : monitor.Summary;
                    break;
            }
            if (info != null) {
                EditorGUILayout.HelpBox(info, MessageType.None);
                Repaint();
            }
        }
    }
}
