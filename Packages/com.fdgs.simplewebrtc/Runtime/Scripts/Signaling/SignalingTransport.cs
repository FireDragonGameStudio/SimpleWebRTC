using System;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// Base class for signaling transports that can be assigned in the inspector.
    /// Derive from it to plug in your own signaling backend. Use <see cref="SetState"/> and
    /// <see cref="ReceiveMessage"/> from any thread, events are always raised on the main thread.
    /// </summary>
    public abstract class SignalingTransport : SimpleWebRTCBehaviour, ISignalingTransport {

        [Tooltip("Raised whenever the signaling connection state changes.")]
        public UnityEvent<SignalingState> StateChangedEvent = new UnityEvent<SignalingState>();

        public SignalingState State { get; private set; } = SignalingState.Disconnected;
        public bool IsConnected => State == SignalingState.Connected;

        public event Action<SignalingState> StateChanged;
        public event Action<string> MessageReceived;

        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(string message);

        [InspectorButton("Connect")]
        private void ConnectFromInspector() => Connect();

        [InspectorButton("Disconnect")]
        private void DisconnectFromInspector() => Disconnect();

        protected void SetState(SignalingState state) {
            MainThreadDispatcher.Run(() => {
                if (State == state) {
                    return;
                }
                State = state;
                SimpleWebRTCLogger.Log($"{GetType().Name} on {name}: {state}");
                StateChanged?.Invoke(state);
                StateChangedEvent?.Invoke(state);
            });
        }

        protected void ReceiveMessage(string message) {
            MainThreadDispatcher.Run(() => MessageReceived?.Invoke(message));
        }

        protected virtual void OnDestroy() {
            if (State != SignalingState.Disconnected) {
                Disconnect();
            }
        }
    }
}
