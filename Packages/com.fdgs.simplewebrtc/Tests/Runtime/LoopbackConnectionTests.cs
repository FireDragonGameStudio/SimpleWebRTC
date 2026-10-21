using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimpleWebRTC.Tests {
    /// <summary>
    /// End-to-end tests over <see cref="LoopbackSignaling"/>, so no server is needed.
    /// </summary>
    public class LoopbackConnectionTests {

        private const float timeoutSeconds = 15f;

        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private string channel;

        private class Client {
            public WebRTCPeerManager Manager;
            public LoopbackSignaling Signaling;
            public DataChannel Data;
            public readonly List<string> Received = new List<string>();
        }

        [SetUp]
        public void SetUp() {
            channel = System.Guid.NewGuid().ToString();
        }

        [TearDown]
        public void TearDown() {
            foreach (var go in createdObjects) {
                Object.Destroy(go);
            }
            createdObjects.Clear();
        }

        private Client CreateClient(string name) {
            var go = new GameObject(name);
            createdObjects.Add(go);
            var client = new Client {
                Signaling = go.AddComponent<LoopbackSignaling>(),
                Manager = go.AddComponent<WebRTCPeerManager>()
            };
            client.Signaling.Channel = channel;
            client.Manager.SetSignaling(client.Signaling);
            client.Manager.SetLocalPeerId(name);
            client.Data = go.AddComponent<DataChannel>();
            client.Data.PeerMessageReceived += (peer, message) => client.Received.Add(message);
            return client;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, string description) {
            var timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition()) {
                if (Time.realtimeSinceStartup > timeout) {
                    Assert.Fail($"Timed out waiting for: {description}");
                }
                yield return null;
            }
        }

        private static bool AllConnected(IEnumerable<Client> clients, int peerCount) {
            return clients.All(c => c.Manager.ConnectedPeerCount == peerCount && c.Data.OpenChannelCount == peerCount);
        }

        [UnityTest]
        public IEnumerator TwoPeers_ConnectAndExchangeMessages() {
            var red = CreateClient("Red");
            var blue = CreateClient("Blue");

            red.Manager.Connect();
            blue.Manager.Connect();
            yield return WaitUntil(() => AllConnected(new[] { red, blue }, 1), "both peers connected with open data channels");

            red.Data.Send("hello blue");
            blue.Data.SendTo(red.Manager.LocalPeerId, "hello red");
            yield return WaitUntil(() => blue.Received.Contains("hello blue") && red.Received.Contains("hello red"), "messages received");
        }

        [UnityTest]
        public IEnumerator LateJoiner_ConnectsToAllExistingPeers() {
            var red = CreateClient("Red");
            var green = CreateClient("Green");
            red.Manager.Connect();
            green.Manager.Connect();
            yield return WaitUntil(() => AllConnected(new[] { red, green }, 1), "first two peers connected");

            var blue = CreateClient("Blue");
            blue.Manager.Connect();
            yield return WaitUntil(() => AllConnected(new[] { red, green, blue }, 2), "full mesh of three peers");

            blue.Data.Send("hi all");
            yield return WaitUntil(() => red.Received.Contains("hi all") && green.Received.Contains("hi all"), "broadcast received");
        }

        [UnityTest]
        public IEnumerator Reconnect_AfterDisconnect_Works() {
            var red = CreateClient("Red");
            var blue = CreateClient("Blue");
            red.Manager.Connect();
            blue.Manager.Connect();
            yield return WaitUntil(() => AllConnected(new[] { red, blue }, 1), "initial connection");

            blue.Manager.Disconnect();
            yield return WaitUntil(() => red.Manager.Sessions.Count == 0 && blue.Manager.Sessions.Count == 0, "sessions closed after leave");

            blue.Manager.Connect();
            yield return WaitUntil(() => AllConnected(new[] { red, blue }, 1), "reconnected");

            red.Data.Send("welcome back");
            yield return WaitUntil(() => blue.Received.Contains("welcome back"), "message after reconnect");
        }

        [UnityTest]
        public IEnumerator AsymmetricPeers_Connect([Values(true, false)] bool senderIsPolite, [Values(true, false)] bool senderJoinsFirst) {
            // different m-line layouts on both sides (audio + data vs. data only) must not break the first negotiation;
            // the polite peer is the one with the lower peer id
            var sender = CreateClient(senderIsPolite ? "A-Sender" : "B-Sender");
            var audioObject = new GameObject("Audio", typeof(AudioSource));
            audioObject.transform.SetParent(sender.Manager.transform);
            var audioSender = sender.Manager.gameObject.AddComponent<AudioSender>();
            audioSender.AudioSource = audioObject.GetComponent<AudioSource>();
            audioSender.StartStreaming();
            var receiver = CreateClient(senderIsPolite ? "B-Receiver" : "A-Receiver");
            receiver.Manager.gameObject.AddComponent<AudioReceiver>();

            if (senderJoinsFirst) {
                sender.Manager.Connect();
                receiver.Manager.Connect();
            } else {
                receiver.Manager.Connect();
                sender.Manager.Connect();
            }
            yield return WaitUntil(() => AllConnected(new[] { sender, receiver }, 1), "asymmetric peers connected");

            receiver.Data.Send("ping");
            yield return WaitUntil(() => sender.Received.Contains("ping"), "message received");
        }

        [UnityTest]
        public IEnumerator DifferentRooms_DoNotConnect() {
            var red = CreateClient("Red");
            var blue = CreateClient("Blue");
            blue.Manager.SetRoom("other room");

            red.Manager.Connect();
            blue.Manager.Connect();
            var until = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < until) {
                yield return null;
            }
            Assert.AreEqual(0, red.Manager.Sessions.Count);
            Assert.AreEqual(0, blue.Manager.Sessions.Count);
        }
    }
}
