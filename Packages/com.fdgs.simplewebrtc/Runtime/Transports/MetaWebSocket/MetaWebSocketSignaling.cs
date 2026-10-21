using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Meta.Net.NativeWebSocket;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Signaling over a WebSocket relay server using the NativeWebSocket fork bundled with the Meta Voice SDK
    /// (Meta.Net.NativeWebSocket). Use this on Meta Quest projects that already include the Voice SDK.
    /// The server only has to broadcast every message to all other clients, see Runtime/SignalServerCode.txt.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Signaling/WebSocket Signaling (Meta Voice SDK)")]
    public class MetaWebSocketSignaling : SignalingTransport {

        [SerializeField] private string serverUrl = "ws://localhost:3000";
        [Tooltip("Sends a user-agent header. Some hosts (e.g. glitch.com) reject connections without it.")]
        [SerializeField] private bool sendUserAgentHeader = true;

        private WebSocket webSocket;

        // Meta's WebSocket sends from background threads, a single queue keeps the message order
        private readonly ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
        private bool isSending;

        public string ServerUrl {
            get => serverUrl;
            set => serverUrl = value;
        }

        public override async void Connect() {
            if (State != SignalingState.Disconnected) {
                return;
            }
            SetState(SignalingState.Connecting);

            try {
                webSocket = sendUserAgentHeader
                    ? new WebSocket(serverUrl, new Dictionary<string, string> { { "user-agent", "unity webrtc" } })
                    : new WebSocket(serverUrl);
            } catch (Exception e) {
                SimpleWebRTCLogger.LogError($"{name}: invalid server url '{serverUrl}'. {e.Message}");
                SetState(SignalingState.Disconnected);
                return;
            }

            var socket = webSocket;
            // callbacks may arrive on background threads, SetState and ReceiveMessage dispatch to the main thread
            socket.OnOpen += () => SetState(SignalingState.Connected);
            socket.OnMessage += (bytes, offset, length) => ReceiveMessage(Encoding.UTF8.GetString(bytes, offset, length));
            socket.OnError += error => SimpleWebRTCLogger.LogError($"WebSocket error: {error}");
            socket.OnClose += code => {
                MainThreadDispatcher.Run(() => {
                    if (webSocket == socket) {
                        webSocket = null;
                    }
                });
                SetState(SignalingState.Disconnected);
            };

            try {
                await socket.Connect();
            } catch (Exception e) {
                SimpleWebRTCLogger.LogError($"{name}: could not connect to '{serverUrl}'. {e.Message}");
                if (webSocket == socket) {
                    webSocket = null;
                }
                SetState(SignalingState.Disconnected);
            }
        }

        public override async void Disconnect() {
            var socket = webSocket;
            webSocket = null;
            while (sendQueue.TryDequeue(out _)) {
            }
            if (socket == null || socket.State == WebSocketState.Closed) {
                SetState(SignalingState.Disconnected);
                return;
            }
            SetState(SignalingState.Disconnecting);
            try {
                await socket.Close();
            } catch (Exception e) {
                SimpleWebRTCLogger.LogWarning($"{name}: error while closing the WebSocket. {e.Message}");
            }
            SetState(SignalingState.Disconnected);
        }

        public override void Send(string message) {
            if (webSocket == null || webSocket.State != WebSocketState.Open) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, WebSocket is not open.");
                return;
            }
            sendQueue.Enqueue(message);
            if (!isSending) {
                SendQueued();
            }
        }

        private async void SendQueued() {
            isSending = true;
            while (sendQueue.TryDequeue(out var message)) {
                var socket = webSocket;
                if (socket == null || socket.State != WebSocketState.Open) {
                    break;
                }
                try {
                    await socket.SendText(message);
                } catch (Exception e) {
                    SimpleWebRTCLogger.LogError($"{name}: send failed. {e.Message}");
                }
            }
            isSending = false;
        }
    }
}
