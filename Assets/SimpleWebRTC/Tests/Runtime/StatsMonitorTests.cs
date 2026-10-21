using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace SimpleWebRTC.Tests {
    public class StatsMonitorTests {

        private const float timeoutSeconds = 15f;

        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private string channel;

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

        private WebRTCPeerManager CreatePeer(string name) {
            var go = new GameObject(name);
            createdObjects.Add(go);
            var signaling = go.AddComponent<LoopbackSignaling>();
            signaling.Channel = channel;
            var manager = go.AddComponent<WebRTCPeerManager>();
            manager.SetSignaling(signaling);
            manager.SetLocalPeerId(name);
            go.AddComponent<DataChannel>();
            return manager;
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

        [UnityTest]
        public IEnumerator Monitor_ReportsNetworkPathAndDataChannelTraffic() {
            var red = CreatePeer("Red");
            var blue = CreatePeer("Blue");
            var monitor = red.gameObject.AddComponent<WebRTCStatsMonitor>();
            PeerStats latest = null;
            monitor.StatsUpdated += s => latest = s;

            red.Connect();
            blue.Connect();
            yield return WaitUntil(() => red.ConnectedPeerCount == 1 && red.GetComponent<DataChannel>().OpenChannelCount == 1, "connection");

            red.GetComponent<DataChannel>().Send("hello");
            yield return WaitUntil(() => latest != null && latest.DataChannelMessagesSent > 0 && latest.RoundTripTimeMs >= 0, "stats with data channel traffic");

            Assert.AreEqual(blue.LocalPeerId, latest.PeerId);
            Assert.IsNotEmpty(latest.LocalCandidateType);
            Assert.IsTrue(monitor.TryGetStats(blue.LocalPeerId, out _));
            StringAssert.Contains(blue.LocalPeerId, monitor.Summary);
        }

        [UnityTest]
        public IEnumerator Monitor_ReportsVideoCodecBitrateAndFrameRate() {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) {
                Assert.Ignore("Video needs a graphics device.");
            }
            var sender = CreatePeer("Sender");
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(sender.transform);
            var source = sender.gameObject.AddComponent<CameraVideoSource>();
            source.SourceCamera = camera;
            source.Resolution = new Vector2Int(640, 360);
            var videoSender = sender.gameObject.AddComponent<VideoSender>();
            videoSender.Source = source;

            var receiver = CreatePeer("Receiver");
            receiver.gameObject.AddComponent<VideoReceiver>();
            var monitor = receiver.gameObject.AddComponent<WebRTCStatsMonitor>();

            sender.Connect();
            receiver.Connect();
            yield return WaitUntil(() => sender.ConnectedPeerCount == 1, "connection");
            videoSender.StartStreaming();

            StreamStats video = null;
            yield return WaitUntil(() => {
                video = monitor.Stats.Values.SelectMany(s => s.Streams)
                    .FirstOrDefault(s => s.IsVideo && s.Direction == StreamDirection.Inbound && s.FramesPerSecond > 0);
                return video != null;
            }, "inbound video stats");

            Assert.AreEqual("VP8", video.Codec);
            Assert.Greater(video.BitrateKbps, 0);
            // the encoder may scale down under load, so only check that a resolution is reported
            Assert.Greater(video.Width, 0);
            Assert.LessOrEqual(video.Width, 640);
        }
    }
}
