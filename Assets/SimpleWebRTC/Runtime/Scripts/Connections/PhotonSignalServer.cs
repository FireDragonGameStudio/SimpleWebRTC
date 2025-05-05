#if FUSION2
using Fusion;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace SimpleWebRTC {
#if FUSION2
    public class PhotonSignalServer : NetworkBehaviour {

        [SerializeField] private WebRTCConnection_Generic webRTCConnection;

        Dictionary<string, Dictionary<int, byte[]>> receivedChunks = new Dictionary<string, Dictionary<int, byte[]>>();

        public async void DisconnectFromServer() {
            await Runner.Shutdown();
        }

        public void HandleMessage(string messageId, int chunkIndex, int totalChunks, byte[] chunkData) {
            RPC_HandleMessage(messageId, chunkIndex, totalChunks, chunkData);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HandleMessage(string messageId, int chunkIndex, int totalChunks, byte[] chunkData) {
            if (!webRTCConnection.PeerId.Equals(messageId)) {
                if (!receivedChunks.ContainsKey(messageId))
                    receivedChunks[messageId] = new Dictionary<int, byte[]>();

                receivedChunks[messageId][chunkIndex] = chunkData;

                if (receivedChunks[messageId].Count == totalChunks) {
                    // Reassemble
                    List<byte> fullMessage = new List<byte>();
                    for (int i = 0; i < totalChunks; i++) {
                        fullMessage.AddRange(receivedChunks[messageId][i]);
                    }
                    byte[] completeMessage = fullMessage.ToArray();

                    // Handle full message
                    webRTCConnection.ReceiveMessage(completeMessage);

                    // for debugging
                    //string fullText = Encoding.UTF8.GetString(completeMessage);
                    //Debug.Log($"Full message received: {fullText}");

                    receivedChunks.Remove(messageId);
                }
            }
        }

        // for rpc testing
        //public void TestMessage() {
        //    RPC_TestMessage();
        //}

        //[Rpc(RpcSources.All, RpcTargets.All)]
        //private void RPC_TestMessage() {
        //    Debug.Log($"RPC called on {webRTCConnection.PeerId}");
        //}
    }
#else
    public class PhotonSignalServer : MonoBehaviour {
        public void HandleMessage(string messageId, int chunkIndex, int totalChunks, byte[] chunkData) {
            Debug.LogError("No Photon Fusion 2 Signal Server! Make sure to install and setup Photon Fusion 2");
        }
    }
#endif
}