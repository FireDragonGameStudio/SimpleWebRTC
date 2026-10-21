using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// A data channel to every connected peer. Channels are pre-negotiated: all peers need a DataChannel component
    /// with the same <see cref="ChannelId"/>. Add several components with different ids for e.g. a reliable chat
    /// channel and an unreliable, unordered channel for fast position updates.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Data Channel")]
    public class DataChannel : PeerSessionComponent {

        [SerializeField] private string label = "data";
        [Tooltip("Must be identical on all peers and unique per peer manager.")]
        [SerializeField, Range(0, 1023)] private int channelId = 0;
        [SerializeField] private bool ordered = true;
        [Tooltip("-1 = reliable (unlimited retransmits). 0 or more makes the channel unreliable.")]
        [SerializeField] private int maxRetransmits = -1;

        [Header("Events")]
        [Tooltip("The channel to a peer is open. Parameter: remote peer id.")]
        public UnityEvent<string> ChannelOpened = new UnityEvent<string>();
        [Tooltip("The channel to a peer closed. Parameter: remote peer id.")]
        public UnityEvent<string> ChannelClosed = new UnityEvent<string>();
        [Tooltip("A message was received (UTF-8 decoded).")]
        public UnityEvent<string> MessageReceived = new UnityEvent<string>();

        /// <summary>A message was received: remote peer id, UTF-8 decoded message.</summary>
        public event Action<string, string> PeerMessageReceived;
        /// <summary>A message was received: remote peer id, raw bytes.</summary>
        public event Action<string, byte[]> BytesReceived;

        private readonly Dictionary<string, RTCDataChannel> channels = new Dictionary<string, RTCDataChannel>();

        public string Label => label;
        public int ChannelId => channelId;
        public int OpenChannelCount => channels.Values.Count(c => c.ReadyState == RTCDataChannelState.Open);

        public bool IsOpen(string remotePeerId) {
            return channels.TryGetValue(remotePeerId, out var channel) && channel.ReadyState == RTCDataChannelState.Open;
        }

        /// <summary>Sends a text message to all peers with an open channel.</summary>
        public void Send(string message) {
            foreach (var pair in channels) {
                if (pair.Value.ReadyState == RTCDataChannelState.Open) {
                    pair.Value.Send(message);
                }
            }
            SimpleWebRTCLogger.LogDataChannel($"{label}: sent '{message}' to {OpenChannelCount} peer(s).");
        }

        /// <summary>Sends binary data to all peers with an open channel.</summary>
        public void Send(byte[] data) {
            foreach (var pair in channels) {
                if (pair.Value.ReadyState == RTCDataChannelState.Open) {
                    pair.Value.Send(data);
                }
            }
        }

        public bool SendTo(string remotePeerId, string message) {
            if (!IsOpen(remotePeerId)) {
                SimpleWebRTCLogger.LogWarning($"{label}: channel to {remotePeerId} is not open.");
                return false;
            }
            channels[remotePeerId].Send(message);
            SimpleWebRTCLogger.LogDataChannel($"{label}: sent '{message}' to {remotePeerId}.");
            return true;
        }

        public bool SendTo(string remotePeerId, byte[] data) {
            if (!IsOpen(remotePeerId)) {
                SimpleWebRTCLogger.LogWarning($"{label}: channel to {remotePeerId} is not open.");
                return false;
            }
            channels[remotePeerId].Send(data);
            return true;
        }

        [InspectorButton("Send Test Message")]
        public void SendTestMessage() {
            Send($"Test message from {(peerManager != null ? peerManager.LocalPeerId : name)}");
        }

        protected override void OnSessionCreated(PeerSession session) {
            if (channels.ContainsKey(session.RemotePeerId)) {
                return;
            }
            var remotePeerId = session.RemotePeerId;
            var channel = session.CreateNegotiatedDataChannel(label, channelId, ordered, maxRetransmits);
            channel.OnOpen = () => {
                SimpleWebRTCLogger.LogDataChannel($"{label}: open to {remotePeerId}.");
                ChannelOpened?.Invoke(remotePeerId);
            };
            channel.OnClose = () => {
                SimpleWebRTCLogger.LogDataChannel($"{label}: closed to {remotePeerId}.");
                ChannelClosed?.Invoke(remotePeerId);
            };
            channel.OnMessage = bytes => {
                var message = Encoding.UTF8.GetString(bytes);
                SimpleWebRTCLogger.LogDataChannel($"{label}: received '{message}' from {remotePeerId}.");
                BytesReceived?.Invoke(remotePeerId, bytes);
                PeerMessageReceived?.Invoke(remotePeerId, message);
                MessageReceived?.Invoke(message);
            };
            channels[remotePeerId] = channel;
        }

        protected override void OnSessionClosed(PeerSession session) {
            if (!channels.TryGetValue(session.RemotePeerId, out var channel)) {
                return;
            }
            channels.Remove(session.RemotePeerId);
            channel.OnMessage = null;
            channel.OnOpen = null;
            channel.OnClose = null;
            channel.Close();
            channel.Dispose();
            ChannelClosed?.Invoke(session.RemotePeerId);
        }
    }
}
