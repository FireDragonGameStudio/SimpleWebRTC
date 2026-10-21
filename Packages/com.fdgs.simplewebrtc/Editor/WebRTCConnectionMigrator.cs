#pragma warning disable 618 // the legacy components are obsolete on purpose
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.WebRTC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SimpleWebRTC.Editor {
    /// <summary>
    /// Converts SimpleWebRTC 1.x WebRTCConnection / WebRTCConnection_Generic components into the 2.x components,
    /// including their settings, UnityEvent listeners and UnityEvent calls that targeted them.
    /// </summary>
    public static class WebRTCConnectionMigrator {

        private const string menuPath = "Tools/SimpleWebRTC/Migrate Open Scenes";

        // 1.x method name -> (2.x component kind, 2.x method name)
        private static readonly Dictionary<string, (string component, string method)> methodMap = new Dictionary<string, (string, string)> {
            { "Connect", ("manager", nameof(WebRTCPeerManager.Connect)) },
            { "ConnectWebRTC", ("manager", nameof(WebRTCPeerManager.Connect)) },
            { "Disconnect", ("manager", nameof(WebRTCPeerManager.Disconnect)) },
            { "SetUniquePlayerName", ("manager", nameof(WebRTCPeerManager.SetLocalPeerId)) },
            { "SendDataChannelMessage", ("data", nameof(DataChannel.Send)) },
            { "WebSocketTestMessage", ("data", nameof(DataChannel.SendTestMessage)) },
            { "StartVideoTransmission", ("video", nameof(VideoSender.StartStreaming)) },
            { "StopVideoTransmission", ("video", nameof(VideoSender.StopStreaming)) },
            { "StartAudioTransmission", ("audio", nameof(AudioSender.StartStreaming)) },
            { "StopAudioTransmission", ("audio", nameof(AudioSender.StopStreaming)) },
        };

        private class Result {
            public MonoBehaviour Legacy;
            public WebRTCPeerManager Manager;
            public SignalingTransport Transport;
            public DataChannel DataChannel;
            public VideoSender VideoSender;
            public AudioSender AudioSender;
            public VideoReceiver VideoReceiver;
            public AudioReceiver AudioReceiver;
            public Camera StreamingCamera;
            public Transform SpectatorCamera;

            public Component Resolve(string kind) {
                switch (kind) {
                    case "manager": return Manager;
                    case "data": return DataChannel;
                    case "video": return VideoSender;
                    case "audio": return AudioSender;
                    default: return null;
                }
            }

            public IEnumerable<Component> All => new Component[] { Manager, Transport, DataChannel, VideoSender, AudioSender, VideoReceiver, AudioReceiver }.Where(c => c != null);
        }

        [MenuItem(menuPath)]
        public static void MigrateOpenScenes() {
            var migrated = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++) {
                migrated += MigrateScene(SceneManager.GetSceneAt(i));
            }
            var message = migrated == 0
                ? "No SimpleWebRTC 1.x components found in the open scenes."
                : $"Migrated {migrated} SimpleWebRTC 1.x component(s). Check the Console for details and save the scene(s).";
            Debug.Log($"[SimpleWebRTC] {message}");
            if (!Application.isBatchMode) {
                EditorUtility.DisplayDialog("SimpleWebRTC Migration", message, "OK");
            }
        }

        [MenuItem(menuPath, true)]
        private static bool CanMigrate() {
            return !Application.isPlaying;
        }

        /// <summary>
        /// Migrates all legacy components of a loaded scene. Returns the number of migrated components.
        /// </summary>
        public static int MigrateScene(Scene scene) {
            if (!scene.isLoaded) {
                return 0;
            }
            var legacyComponents = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c => c is WebRTCConnection || c is WebRTCConnection_Generic)
                .ToList();
            if (legacyComponents.Count == 0) {
                return 0;
            }

            Undo.SetCurrentGroupName("Migrate SimpleWebRTC components");
            var results = new List<Result>();
            foreach (var legacy in legacyComponents) {
                var result = Migrate(legacy);
                if (result != null) {
                    results.Add(result);
                }
            }

            var sceneComponents = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c => c != null)
                .ToList();
            foreach (var result in results) {
                RetargetPersistentCalls(sceneComponents, result);
                MigrateSampleScripts(sceneComponents, result);
            }
            if (results.Count == 1) {
                AutoWireReferences(sceneComponents, results[0]);
            }

            foreach (var result in results) {
                Undo.DestroyObjectImmediate(result.Legacy);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return results.Count;
        }

        public static bool Migrate(MonoBehaviour legacy, bool destroyLegacy) {
            var result = Migrate(legacy);
            if (result == null) {
                return false;
            }
            var sceneComponents = legacy.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .ToList();
            RetargetPersistentCalls(sceneComponents, result);
            MigrateSampleScripts(sceneComponents, result);
            if (destroyLegacy) {
                Undo.DestroyObjectImmediate(legacy);
            }
            EditorSceneManager.MarkSceneDirty(legacy.gameObject.scene);
            return true;
        }

        private static Result Migrate(MonoBehaviour legacy) {
            var go = legacy.gameObject;
            var old = new SerializedObject(legacy);
            var isGeneric = legacy is WebRTCConnection_Generic;
            var result = new Result { Legacy = legacy };

            // signaling transport
            if (isGeneric) {
                var photonType = FindType("SimpleWebRTC.PhotonFusionSignaling");
                if (photonType == null) {
                    Debug.LogError($"[SimpleWebRTC] '{go.name}' uses Photon Fusion signaling. Install Photon Fusion 2 (FUSION2 define) and migrate again.", go);
                    return null;
                }
                result.Transport = (SignalingTransport)Undo.AddComponent(go, photonType);
                var relay = old.FindProperty("SignalServerMessageHandler").objectReferenceValue as MonoBehaviour;
                if (relay != null && relay.GetType().Name == "PhotonSignalingRelay") {
                    Set(result.Transport, "relay", relay);
                    Set(relay, "transport", result.Transport);
                }
            } else {
                var webSocketType = FindType("SimpleWebRTC.WebSocketSignaling") ?? FindType("SimpleWebRTC.MetaWebSocketSignaling");
                if (webSocketType != null) {
                    result.Transport = (SignalingTransport)Undo.AddComponent(go, webSocketType);
                    Set(result.Transport, "serverUrl", old.FindProperty("WebSocketServerAddress").stringValue);
                    Set(result.Transport, "sendUserAgentHeader", old.FindProperty("UseHTTPHeader").boolValue);
                } else {
                    Debug.LogWarning($"[SimpleWebRTC] No WebSocket package installed, '{go.name}' gets a LoopbackSignaling instead.", go);
                    result.Transport = Undo.AddComponent<LoopbackSignaling>(go);
                }
            }

            // peer manager
            var manager = result.Manager = Undo.AddComponent<WebRTCPeerManager>(go);
            Set(manager, "signaling", result.Transport);
            Set(manager, "localPeerId", old.FindProperty("LocalPeerId").stringValue.Replace("-PeerId", string.Empty));
            Set(manager, "appendRandomSuffix", old.FindProperty("RandomUniquePeerId").boolValue);
            Set(manager, "showLogs", old.FindProperty("ShowLogs").boolValue);
            Set(manager, "showDataChannelLogs", old.FindProperty("ShowDataChannelLogs").boolValue);
            var activeOnStart = old.FindProperty("WebSocketConnectionActive");
            Set(manager, "connectOnStart", activeOnStart != null && activeOnStart.boolValue);
            var stun = old.FindProperty("StunServerAddress").stringValue;
            var managerObject = new SerializedObject(manager);
            var iceServers = managerObject.FindProperty("iceServers");
            iceServers.arraySize = string.IsNullOrEmpty(stun) ? 0 : 1;
            if (iceServers.arraySize == 1) {
                var urls = iceServers.GetArrayElementAtIndex(0).FindPropertyRelative("urls");
                urls.arraySize = 1;
                urls.GetArrayElementAtIndex(0).stringValue = stun;
            }
            managerObject.ApplyModifiedPropertiesWithoutUndo();
            CopyCalls(old, "WebSocketConnectionChanged", manager, nameof(WebRTCPeerManager.SignalingStateChanged));
            CopyCalls(old, "WebRTCConnected", manager, nameof(WebRTCPeerManager.PeerConnected));

            // data channel
            result.DataChannel = Undo.AddComponent<DataChannel>(go);
            Set(result.DataChannel, "peerManager", manager);
            CopyCalls(old, "DataChannelConnected", result.DataChannel, nameof(DataChannel.ChannelOpened));
            CopyCalls(old, "DataChannelMessageReceived", result.DataChannel, nameof(DataChannel.MessageReceived));

            var immersive = !isGeneric && old.FindProperty("UseImmersiveSetup").boolValue;
            result.StreamingCamera = old.FindProperty("StreamingCamera").objectReferenceValue as Camera;
            result.SpectatorCamera = isGeneric ? null : old.FindProperty("experimentalSpectatorCam6DOF").objectReferenceValue as Transform;

            // sending
            if (old.FindProperty("IsVideoAudioSender").boolValue) {
                VideoSource source;
                if (immersive) {
                    source = Undo.AddComponent<CubemapVideoSource>(go);
                    Set(source, "sourceCamera", result.StreamingCamera);
                    Set(source, "renderStereo", old.FindProperty("RenderStereo").boolValue);
                    Set(source, "stereoSeparation", old.FindProperty("StereoSeparation").floatValue);
                    Set(source, "eyeResolution", old.FindProperty("OneEyeRenderSide").intValue);
                    Set(source, "depth", ToDepth(old.FindProperty("RTDepth").intValue));
                    Set(source, "oneFacePerFrame", old.FindProperty("OneFacePerFrame").boolValue);
                } else {
                    source = Undo.AddComponent<CameraVideoSource>(go);
                    Set(source, "sourceCamera", result.StreamingCamera);
                    Set(source, "resolution", old.FindProperty("VideoResolution").vector2IntValue);
                }
                result.VideoSender = Undo.AddComponent<VideoSender>(go);
                Set(result.VideoSender, "peerManager", manager);
                Set(result.VideoSender, "source", source);
                Set(result.VideoSender, "previewImage", old.FindProperty("OptionalPreviewRawImage").objectReferenceValue);

                var audioSource = old.FindProperty("StreamingAudioSource").objectReferenceValue as AudioSource;
                if (audioSource != null) {
                    result.AudioSender = Undo.AddComponent<AudioSender>(go);
                    Set(result.AudioSender, "peerManager", manager);
                    Set(result.AudioSender, "audioSource", audioSource);
                }
            }

            // receiving
            if (old.FindProperty("IsVideoAudioReceiver").boolValue) {
                VideoSink sink;
                if (immersive) {
                    sink = Undo.AddComponent<RenderTextureVideoSink>(go);
                    Set(sink, "targetTexture", old.FindProperty("receivingRenderTexture").objectReferenceValue);
                } else {
                    sink = Undo.AddComponent<RawImageVideoSink>(go);
                    Set(sink, "spawnParent", old.FindProperty("ReceivingRawImagesParent").objectReferenceValue);
                }
                result.VideoReceiver = Undo.AddComponent<VideoReceiver>(go);
                Set(result.VideoReceiver, "peerManager", manager);
                var receiverObject = new SerializedObject(result.VideoReceiver);
                var sinks = receiverObject.FindProperty("sinks");
                sinks.arraySize = 1;
                sinks.GetArrayElementAtIndex(0).objectReferenceValue = sink;
                receiverObject.ApplyModifiedPropertiesWithoutUndo();
                CopyCalls(old, "VideoTransmissionReceived", result.VideoReceiver, nameof(VideoReceiver.VideoStarted));

                result.AudioReceiver = Undo.AddComponent<AudioReceiver>(go);
                Set(result.AudioReceiver, "peerManager", manager);
                Set(result.AudioReceiver, "spawnParent", old.FindProperty("ReceivingAudioSourceParent").objectReferenceValue);
                CopyCalls(old, "AudioTransmissionReceived", result.AudioReceiver, nameof(AudioReceiver.AudioStarted));
            }

            Debug.Log($"[SimpleWebRTC] Migrated '{go.name}': {string.Join(", ", result.All.Select(c => c.GetType().Name))}.", go);
            return result;
        }

        /// <summary>
        /// UnityEvent calls (e.g. from buttons) that targeted the legacy component now target the new components.
        /// </summary>
        private static void RetargetPersistentCalls(List<MonoBehaviour> components, Result result) {
            foreach (var component in components) {
                if (component == null || component == result.Legacy) {
                    continue;
                }
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                var changed = false;
                while (property.Next(true)) {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name != "m_Target"
                        || property.objectReferenceValue != result.Legacy) {
                        continue;
                    }
                    var call = property.propertyPath.Substring(0, property.propertyPath.Length - "m_Target".Length);
                    var methodProperty = serializedObject.FindProperty(call + "m_MethodName");
                    if (!methodMap.TryGetValue(methodProperty.stringValue, out var mapping) || result.Resolve(mapping.component) == null) {
                        Debug.LogWarning($"[SimpleWebRTC] '{component.name}' calls {methodProperty.stringValue} on the old component, which has no 2.x equivalent. Please rewire it.", component);
                        continue;
                    }
                    var newTarget = result.Resolve(mapping.component);
                    property.objectReferenceValue = newTarget;
                    serializedObject.FindProperty(call + "m_TargetAssemblyTypeName").stringValue = TypeName(newTarget.GetType());
                    methodProperty.stringValue = mapping.method;
                    changed = true;
                }
                if (changed) {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static void MigrateSampleScripts(List<MonoBehaviour> components, Result result) {
            foreach (var cameraSwitch in components.OfType<SwitchToNextCameraPosition>()) {
                Set(cameraSwitch, "peerManager", result.Manager);
                Set(cameraSwitch, "dataChannel", result.DataChannel);
                if (result.VideoSender != null) {
                    Set(cameraSwitch, "videoSender", result.VideoSender);
                    Set(cameraSwitch, "streamingCamera", result.StreamingCamera);
                }
                if (result.SpectatorCamera != null) {
                    Set(cameraSwitch, "spectatorCamera", result.SpectatorCamera);
                }
            }
        }

        /// <summary>
        /// With a single client in the scene, empty fields of other scripts that expect one of the new component
        /// types (e.g. a lobby UI) are assigned automatically.
        /// </summary>
        private static void AutoWireReferences(List<MonoBehaviour> components, Result result) {
            var candidates = result.All.ToList();
            foreach (var component in components) {
                if (component == null || component == result.Legacy || candidates.Contains(component) || component is SimpleWebRTCBehaviour) {
                    continue;
                }
                var serializedObject = new SerializedObject(component);
                var changed = false;
                foreach (var field in GetSerializedFields(component.GetType())) {
                    if (!typeof(Component).IsAssignableFrom(field.FieldType)) {
                        continue;
                    }
                    var property = serializedObject.FindProperty(field.Name);
                    if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null) {
                        continue;
                    }
                    var match = candidates.FirstOrDefault(c => field.FieldType.IsInstanceOfType(c));
                    if (match != null) {
                        property.objectReferenceValue = match;
                        changed = true;
                        Debug.Log($"[SimpleWebRTC] Assigned {match.GetType().Name} to {component.GetType().Name}.{field.Name} on '{component.name}'.", component);
                    }
                }
                if (changed) {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static IEnumerable<FieldInfo> GetSerializedFields(Type type) {
            for (; type != null && type != typeof(MonoBehaviour); type = type.BaseType) {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) {
                        yield return field;
                    }
                }
            }
        }

        private static void CopyCalls(SerializedObject source, string sourceEvent, Object target, string targetEvent) {
            var sourceCalls = source.FindProperty(sourceEvent + ".m_PersistentCalls.m_Calls");
            if (sourceCalls == null || sourceCalls.arraySize == 0) {
                return;
            }
            var targetObject = new SerializedObject(target);
            var targetCalls = targetObject.FindProperty(targetEvent + ".m_PersistentCalls.m_Calls");
            foreach (var i in Enumerable.Range(0, sourceCalls.arraySize)) {
                targetCalls.arraySize++;
                CopyProperty(sourceCalls.GetArrayElementAtIndex(i), targetCalls.GetArrayElementAtIndex(targetCalls.arraySize - 1));
            }
            targetObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopyProperty(SerializedProperty source, SerializedProperty target) {
            var sourceEnd = source.GetEndProperty();
            var sourceIterator = source.Copy();
            var basePath = source.propertyPath;
            var enterChildren = true;
            while (sourceIterator.Next(enterChildren) && !SerializedProperty.EqualContents(sourceIterator, sourceEnd)) {
                enterChildren = true;
                var relativePath = sourceIterator.propertyPath.Substring(basePath.Length + 1);
                var targetProperty = target.serializedObject.FindProperty(target.propertyPath + "." + relativePath);
                if (targetProperty == null) {
                    continue;
                }
                switch (sourceIterator.propertyType) {
                    case SerializedPropertyType.ObjectReference:
                        targetProperty.objectReferenceValue = sourceIterator.objectReferenceValue;
                        break;
                    case SerializedPropertyType.String:
                        targetProperty.stringValue = sourceIterator.stringValue;
                        break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.Enum:
                        targetProperty.intValue = sourceIterator.intValue;
                        break;
                    case SerializedPropertyType.Float:
                        targetProperty.floatValue = sourceIterator.floatValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        targetProperty.boolValue = sourceIterator.boolValue;
                        break;
                }
            }
        }

        private static void Set(Object target, string propertyName, object value) {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) {
                Debug.LogError($"[SimpleWebRTC] Migration: {target.GetType().Name} has no property '{propertyName}'.");
                return;
            }
            switch (value) {
                case Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case null:
                    property.objectReferenceValue = null;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case Vector2Int vector:
                    property.vector2IntValue = vector;
                    break;
                case Enum enumValue:
                    property.intValue = Convert.ToInt32(enumValue);
                    break;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RenderTextureDepth ToDepth(int value) {
            return Enum.IsDefined(typeof(RenderTextureDepth), value) ? (RenderTextureDepth)value : RenderTextureDepth.Depth24;
        }

        private static Type FindType(string fullName) {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("SimpleWebRTC"))
                .Select(a => a.GetType(fullName))
                .FirstOrDefault(t => t != null);
        }

        private static string TypeName(Type type) {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }

    [CustomEditor(typeof(WebRTCConnection))]
    internal class WebRTCConnectionEditor : LegacyConnectionEditor {
    }

    [CustomEditor(typeof(WebRTCConnection_Generic))]
    internal class WebRTCConnectionGenericEditor : LegacyConnectionEditor {
    }

    internal abstract class LegacyConnectionEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("This SimpleWebRTC 1.x component does nothing in 2.x. Migrate it to the new components " +
                "(WebRTCPeerManager, signaling transport, DataChannel, video and audio components). Settings, events and button calls are converted.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                if (GUILayout.Button("Migrate to SimpleWebRTC 2.x", GUILayout.Height(30))) {
                    WebRTCConnectionMigrator.Migrate((MonoBehaviour)target, true);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true)) {
                DrawDefaultInspector();
            }
        }
    }
}
