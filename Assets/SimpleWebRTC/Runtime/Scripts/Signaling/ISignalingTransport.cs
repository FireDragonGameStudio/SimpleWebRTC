using System;

namespace SimpleWebRTC {
    public enum SignalingState {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    /// <summary>
    /// Transport for signaling messages (WebSocket, Photon, loopback, your own backend, ...).
    /// Implementations only move strings: they must deliver every sent message to all other connected peers.
    /// Events must be raised on the main thread.
    /// </summary>
    public interface ISignalingTransport {
        SignalingState State { get; }

        event Action<SignalingState> StateChanged;
        event Action<string> MessageReceived;

        void Connect();
        void Disconnect();
        void Send(string message);
    }
}
