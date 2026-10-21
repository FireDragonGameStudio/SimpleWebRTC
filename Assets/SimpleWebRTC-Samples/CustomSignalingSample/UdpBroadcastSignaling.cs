using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SimpleWebRTC.Samples {
    /// <summary>
    /// Example of a custom signaling transport: signaling via UDP broadcast in the local network.
    /// No server needed, every app on the same network and port finds the others automatically.
    ///
    /// How a transport works: <see cref="Send"/> must deliver a string to all other clients, and every string
    /// received from another client goes to <see cref="SignalingTransport.ReceiveMessage"/>. Report the connection
    /// state with <see cref="SignalingTransport.SetState"/>. Both are safe to call from any thread.
    ///
    /// Limitations of this example: local network only (broadcasts do not cross routers), no encryption, and no
    /// retransmission of lost datagrams (rare on a LAN; reconnect if a peer gets stuck). Not available on WebGL.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Samples/UDP Broadcast Signaling")]
    public class UdpBroadcastSignaling : SignalingTransport {

        // every datagram is "<header>|<sender instance>|<sequence>|<message>"
        private const string header = "SWRTC-UDP1";
        private const int maxDatagramSize = 60000;

        [Tooltip("All apps that should find each other use the same port.")]
        [SerializeField] private int port = 47777;
        [Tooltip("Also send to localhost, so several instances on the same machine find each other.")]
        [SerializeField] private bool includeLocalhost = true;

        private readonly string instanceId = Guid.NewGuid().ToString("N").Substring(0, 12);
        private readonly object sendLock = new object();
        // the same datagram can arrive twice (broadcast + localhost), remember the latest ones per sender
        private readonly Dictionary<string, HashSet<long>> receivedSequences = new Dictionary<string, HashSet<long>>();

        private UdpClient client;
        private Thread receiveThread;
        private long sequence;
        private AndroidJavaObject multicastLock;

        public int Port {
            get => port;
            set => port = value;
        }

        public override void Connect() {
            if (State != SignalingState.Disconnected) {
                return;
            }
            SetState(SignalingState.Connecting);
            try {
                AcquireMulticastLock();
                client = new UdpClient { ExclusiveAddressUse = false, EnableBroadcast = true };
                // several apps (or editor + player) on one machine share the port
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            } catch (Exception e) {
                SimpleWebRTCLogger.LogError($"{name}: could not open UDP port {port}. {e.Message}");
                CloseSocket();
                SetState(SignalingState.Disconnected);
                return;
            }

            var socket = client;
            receiveThread = new Thread(() => ReceiveLoop(socket)) { IsBackground = true, Name = "SimpleWebRTC UDP signaling" };
            receiveThread.Start();
            SetState(SignalingState.Connected);
        }

        public override void Disconnect() {
            if (State == SignalingState.Disconnected) {
                return;
            }
            CloseSocket();
            SetState(SignalingState.Disconnected);
        }

        public override void Send(string message) {
            var socket = client;
            if (socket == null) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, UDP signaling is not connected.");
                return;
            }
            var bytes = Encoding.UTF8.GetBytes($"{header}|{instanceId}|{Interlocked.Increment(ref sequence)}|{message}");
            if (bytes.Length > maxDatagramSize) {
                SimpleWebRTCLogger.LogError($"{name}: message of {bytes.Length} bytes is too large for one UDP datagram.");
                return;
            }
            lock (sendLock) {
                try {
                    socket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port));
                    if (includeLocalhost) {
                        socket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, port));
                    }
                } catch (Exception e) when (e is SocketException || e is ObjectDisposedException) {
                    SimpleWebRTCLogger.LogWarning($"{name}: UDP send failed. {e.Message}");
                }
            }
        }

        private void ReceiveLoop(UdpClient socket) {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (true) {
                byte[] bytes;
                try {
                    bytes = socket.Receive(ref remote);
                } catch (ObjectDisposedException) {
                    return;
                } catch (SocketException e) {
                    if (socket != client) {
                        // closed on purpose
                        return;
                    }
                    // e.g. ICMP "port unreachable" on Windows, keep listening
                    SimpleWebRTCLogger.LogWarning($"UDP signaling receive error: {e.Message}");
                    continue;
                }
                HandleDatagram(Encoding.UTF8.GetString(bytes));
            }
        }

        private void HandleDatagram(string datagram) {
            // header|instance|sequence|message, the message itself may contain '|'
            var parts = datagram.Split(new[] { '|' }, 4);
            if (parts.Length != 4 || parts[0] != header || parts[1] == instanceId || !long.TryParse(parts[2], out var number)) {
                return;
            }
            lock (receivedSequences) {
                if (!receivedSequences.TryGetValue(parts[1], out var seen)) {
                    seen = new HashSet<long>();
                    receivedSequences[parts[1]] = seen;
                }
                if (!seen.Add(number)) {
                    return;
                }
                if (seen.Count > 1000) {
                    seen.RemoveWhere(n => n < number - 500);
                }
            }
            ReceiveMessage(parts[3]);
        }

        private void CloseSocket() {
            var socket = client;
            client = null;
            socket?.Close();
            receiveThread = null;
            lock (receivedSequences) {
                receivedSequences.Clear();
            }
            ReleaseMulticastLock();
        }

        // Android (e.g. Meta Quest) drops incoming broadcast packets unless the app holds a Wi-Fi multicast lock.
        // Add <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE"/> to your
        // Assets/Plugins/Android/AndroidManifest.xml (Project Settings > Player > Publishing Settings > Custom Main Manifest).
        private void AcquireMulticastLock() {
#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi")) {
                    multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "SimpleWebRTC");
                    multicastLock.Call("setReferenceCounted", false);
                    multicastLock.Call("acquire");
                }
            } catch (Exception e) {
                SimpleWebRTCLogger.LogWarning($"Could not acquire the Wi-Fi multicast lock, broadcasts may not arrive. {e.Message}");
            }
#endif
        }

        private void ReleaseMulticastLock() {
            if (multicastLock == null) {
                return;
            }
            try {
                multicastLock.Call("release");
            } catch (Exception) {
                // the lock is gone anyway
            }
            multicastLock.Dispose();
            multicastLock = null;
        }
    }
}
