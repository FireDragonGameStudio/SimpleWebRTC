using System;

namespace SimpleWebRTC {
    public class SignalingMessage {

        private const int minMessageParts = 4;

        public readonly SignalingMessageType Type = SignalingMessageType.OTHER;
        public readonly string SenderPeerId = "NOID";
        public readonly string ReceiverPeerId = "NOID";
        public readonly string Message = "Default Value";
        public readonly int ConnectionCount = 0;
        public readonly bool IsVideoAudioSender = true;

        public SignalingMessage(string messageString) {

            var messageArray = messageString.Split("|");

            if ((messageArray.Length >= minMessageParts) && Enum.TryParse(messageArray[0], out SignalingMessageType resultType)) {
                Type = resultType;
                SenderPeerId = messageArray[1];
                ReceiverPeerId = messageArray[2];
                Message = messageArray[3];
                ConnectionCount = int.Parse(messageArray[4]);
                IsVideoAudioSender = bool.Parse(messageArray[5]);
            }
        }
    }
}