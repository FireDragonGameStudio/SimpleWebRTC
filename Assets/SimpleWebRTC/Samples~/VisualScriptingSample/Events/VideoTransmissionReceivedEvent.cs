using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnVideoTransmissionReceived")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class VideoTransmissionReceivedEvent : EventUnit<EmptyEventArgs> {

        [DoNotSerialize]
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.VideoTransmissionReceived);
        }

        protected override void Definition() {
            base.Definition();
        }

        public static void Trigger() {
            EventBus.Trigger(SimpleWebRTCEventNames.VideoTransmissionReceived);
        }
    }
}