using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Network part of <see cref="PhotonFusionSignaling"/>: relays signaling messages as RPCs through a Photon
    /// Fusion 2 session. Put it on a NetworkObject that exists for all players (e.g. a scene object).
    /// Fusion RPCs are limited to 512 bytes, so messages are split into chunks.
    /// Add the SimpleWebRTC.PhotonFusion assembly to NetworkProjectConfig > Assemblies To Weave.
    /// </summary>
    public class PhotonSignalingRelay : NetworkBehaviour {

        private const int maxChunkSize = 400;

        [SerializeField] private PhotonFusionSignaling transport;

        private readonly Dictionary<string, byte[][]> receivedChunks = new Dictionary<string, byte[][]>();
        private int messageCounter;

        public bool IsReady { get; private set; }

        public event Action<bool> ReadyChanged;
        public event Action<byte[]> MessageReceived;

        public override void Spawned() {
            if (transport != null) {
                transport.SetRelay(this);
            }
            IsReady = true;
            ReadyChanged?.Invoke(true);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) {
            IsReady = false;
            receivedChunks.Clear();
            ReadyChanged?.Invoke(false);
        }

        public void Send(byte[] message) {
            if (!IsReady) {
                SimpleWebRTCLogger.LogWarning($"{name}: cannot send, the Photon relay is not spawned.");
                return;
            }
            var messageId = $"{Runner.LocalPlayer.PlayerId}-{messageCounter++}";
            var totalChunks = Mathf.Max(1, Mathf.CeilToInt((float)message.Length / maxChunkSize));
            for (var i = 0; i < totalChunks; i++) {
                var offset = i * maxChunkSize;
                var chunk = new byte[Math.Min(maxChunkSize, message.Length - offset)];
                Array.Copy(message, offset, chunk, 0, chunk.Length);
                RPC_Chunk(messageId, i, totalChunks, chunk);
            }
        }

        public async void ShutdownRunner() {
            if (Runner != null) {
                await Runner.Shutdown();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        private void RPC_Chunk(string messageId, int chunkIndex, int totalChunks, byte[] chunk) {
            if (!receivedChunks.TryGetValue(messageId, out var chunks)) {
                chunks = new byte[totalChunks][];
                receivedChunks[messageId] = chunks;
            }
            chunks[chunkIndex] = chunk;

            foreach (var part in chunks) {
                if (part == null) {
                    return;
                }
            }
            receivedChunks.Remove(messageId);

            var length = 0;
            foreach (var part in chunks) {
                length += part.Length;
            }
            var message = new byte[length];
            var position = 0;
            foreach (var part in chunks) {
                Array.Copy(part, 0, message, position, part.Length);
                position += part.Length;
            }
            MessageReceived?.Invoke(message);
        }
    }
}
