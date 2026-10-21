using System;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Signaling over a WebSocket relay server using NativeWebSocket (com.endel.nativewebsocket 2.x).
    /// The server only has to broadcast every message to all other clients, see Runtime/SignalServerCode.txt.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Signaling/WebSocket Signaling (NativeWebSocket)")]
    public class WebSocketSignaling : SignalingTransport {

        [SerializeField] private string serverUrl = "ws://localhost:3000";
        [Tooltip("Sends a user-agent header. Some hosts (e.g. glitch.com) reject connections without it.")]
        [SerializeField] private bool sendUserAgentHeader = true;

        private WebSocket webSocket;

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
            socket.OnOpen += () => SetState(SignalingState.Connected);
            socket.OnMessage += bytes => ReceiveMessage(Encoding.UTF8.GetString(bytes));
            socket.OnError += error => SimpleWebRTCLogger.LogError($"{name}: WebSocket error: {error}");
            socket.OnClose += code => {
                if (webSocket == socket) {
                    webSocket = null;
                }
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

        public override async void Send(string message) {
            if (webSocket == null || webSocket.State != WebSocketState.Open) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, WebSocket is not open.");
                return;
            }
            try {
                // NativeWebSocket 2.x queues outgoing messages itself, so fast sending is safe
                await webSocket.SendText(message);
            } catch (Exception e) {
                SimpleWebRTCLogger.LogError($"{name}: send failed. {e.Message}");
            }
        }
    }
}
