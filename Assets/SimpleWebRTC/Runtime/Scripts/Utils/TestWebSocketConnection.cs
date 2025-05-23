using NativeWebSocket;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleWebRTC {
    public class TestWebSocketConnection : MonoBehaviour {

        [SerializeField] private string webSocketServerAddress = "wss://unity-webrtc-signaling.glitch.me";
        [SerializeField] private bool useHTTPHeader = true;

        private WebSocket webSocket;

        // Start is called before the first frame update
        async void Start() {
            //websocket = new WebSocket("wss://echo.websocket.org/");
            webSocket ??= (useHTTPHeader
                ? new WebSocket(webSocketServerAddress, new Dictionary<string, string>() { { "user-agent", "unity webrtc" } })
                : new WebSocket(webSocketServerAddress));

            webSocket.OnOpen += () => {
                Debug.Log("Connection open!");
            };

            webSocket.OnError += (e) => {
                Debug.Log("Error! " + e);
            };

            webSocket.OnClose += (e) => {
                Debug.Log("Connection closed!");
            };

            webSocket.OnMessage += (bytes) => {
                Debug.Log("OnMessage!");
                Debug.Log(bytes);

                // getting the message as a string
                // var message = System.Text.Encoding.UTF8.GetString(bytes);
                // Debug.Log("OnMessage! " + message);
            };

            // Keep sending messages at every 1s
            InvokeRepeating(nameof(SendWebSocketMessage), 0.0f, 1f);

            // waiting for messages
            await webSocket.Connect();
        }

        void Update() {
#if !UNITY_WEBGL || UNITY_EDITOR
            webSocket.DispatchMessageQueue();
#endif
        }

        async void SendWebSocketMessage() {
            if (webSocket.State == WebSocketState.Open) {
                // Sending bytes
                await webSocket.Send(new byte[] { 10, 20, 30 });

                // Sending plain text
                await webSocket.SendText("plain text message" + DateTime.Now.ToShortTimeString());
            }
        }

        private async void OnApplicationQuit() {
            await webSocket.Close();
        }
    }
}