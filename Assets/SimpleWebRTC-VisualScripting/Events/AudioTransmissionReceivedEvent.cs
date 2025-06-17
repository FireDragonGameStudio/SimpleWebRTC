using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnAudioTransmissionReceived")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class AudioTransmissionReceivedEvent : EventUnit<EmptyEventArgs> {

        [DoNotSerialize]
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.AudioTransmissionReceived);
        }

        protected override void Definition() {
            base.Definition();
        }

        public static void Trigger() {
            EventBus.Trigger(SimpleWebRTCEventNames.AudioTransmissionReceived);
        }
    }
}