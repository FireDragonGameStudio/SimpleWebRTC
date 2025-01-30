using System;

public class SignalingMessage {

    private const int minMessageParts = 4;

    public readonly SignalingMessageType Type = SignalingMessageType.OTHER;
    public readonly string SenderPeerId = "NOID";
    public readonly string ReceiverPeerId = "NOID";
    public readonly string Message = "Default Value";

    public SignalingMessage(string messageString) {

        var messageArray = messageString.Split("|");

        if ((messageArray.Length >= minMessageParts) && Enum.TryParse(messageArray[0], out SignalingMessageType resultType)) {
            switch (resultType) {
                case SignalingMessageType.NEWPEER:
                case SignalingMessageType.NEWPEERACK:
                case SignalingMessageType.OFFER:
                case SignalingMessageType.ANSWER:
                case SignalingMessageType.CANDIDATE:
                case SignalingMessageType.DISPOSE:
                    Type = resultType;
                    SenderPeerId = messageArray[1];
                    ReceiverPeerId = messageArray[2];
                    Message = messageArray[3];
                    break;
                default:
                    Type = SignalingMessageType.OTHER;
                    SenderPeerId = messageArray[1];
                    ReceiverPeerId = messageArray[2];
                    Message = messageArray[3];
                    break;
            }
        }
    }
}