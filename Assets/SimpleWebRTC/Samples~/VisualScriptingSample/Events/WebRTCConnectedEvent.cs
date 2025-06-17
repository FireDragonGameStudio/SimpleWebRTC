using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnWebRTCConnected")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class WebRTCConnectedEvent : EventUnit<EmptyEventArgs> {

        [DoNotSerialize]
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.WebRTCConnected);
        }

        protected override void Definition() {
            base.Definition();
        }

        public static void Trigger() {
            EventBus.Trigger(SimpleWebRTCEventNames.WebRTCConnected);
        }
    }
}