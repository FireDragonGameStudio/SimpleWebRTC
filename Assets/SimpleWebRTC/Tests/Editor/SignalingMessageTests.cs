using NUnit.Framework;

namespace SimpleWebRTC.Tests {
    public class SignalingMessageTests {

        [Test]
        public void RoundTrip_KeepsAllFields() {
            var message = new SignalingMessage(SignalingMessageType.Offer, "room", "Peer-a", "Peer-b", "{\"sdp\":\"v=0|x\"}");

            Assert.IsTrue(SignalingMessage.TryParse(message.ToJson(), out var parsed));
            Assert.AreEqual(SignalingMessageType.Offer, parsed.type);
            Assert.AreEqual("room", parsed.room);
            Assert.AreEqual("Peer-a", parsed.from);
            Assert.AreEqual("Peer-b", parsed.to);
            Assert.AreEqual("{\"sdp\":\"v=0|x\"}", parsed.payload);
        }

        [Test]
        public void EmptyReceiver_IsBroadcast() {
            var message = new SignalingMessage(SignalingMessageType.Join, "room", "Peer-a", null);

            Assert.IsTrue(message.IsBroadcast);
            Assert.IsTrue(message.IsAddressedTo("anyone"));
        }

        [Test]
        public void DirectMessage_IsOnlyAddressedToReceiver() {
            var message = new SignalingMessage(SignalingMessageType.Answer, "room", "Peer-a", "Peer-b");

            Assert.IsTrue(message.IsAddressedTo("Peer-b"));
            Assert.IsFalse(message.IsAddressedTo("Peer-c"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("plain text message")]
        [TestCase("NEWPEER|Peer-a|ALL|New peer|0|True")]
        [TestCase("{\"foo\":1}")]
        [TestCase("{\"v\":1,\"type\":\"offer\",\"from\":\"a\"}")]
        [TestCase("{broken json")]
        public void ForeignOrInvalidMessages_AreRejected(string raw) {
            Assert.IsFalse(SignalingMessage.TryParse(raw, out _));
        }

        [Test]
        public void SessionDescription_ConvertsSdpType() {
            var description = new SessionDescription { type = "answer", sdp = "v=0" };

            var rtcDescription = description.ToRTCSessionDescription();

            Assert.AreEqual(Unity.WebRTC.RTCSdpType.Answer, rtcDescription.type);
            Assert.AreEqual("answer", new SessionDescription(rtcDescription).type);
        }

        [Test]
        public void PeerIds_AreUniqueAndPrefixed() {
            var first = PeerIdGenerator.Create("Red");
            var second = PeerIdGenerator.Create("Red");

            StringAssert.StartsWith("Red-", first);
            Assert.AreNotEqual(first, second);
        }
    }
}
