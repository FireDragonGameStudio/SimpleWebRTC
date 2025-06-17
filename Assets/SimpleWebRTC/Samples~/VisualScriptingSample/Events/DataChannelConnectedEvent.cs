using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnDataChannelConnected")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class DataChannelConnectedEvent : EventUnit<string> {

        [DoNotSerialize]
        public ValueOutput senderPeerId { get; private set; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.DataChannelConnected);
        }

        protected override void Definition() {
            base.Definition();

            senderPeerId = ValueOutput<string>(nameof(senderPeerId));
        }

        protected override void AssignArguments(Flow flow, string data) {
            flow.SetValue(senderPeerId, data);
        }

        public static void Trigger(string eventData) {
            EventBus.Trigger(SimpleWebRTCEventNames.DataChannelConnected, eventData);
        }
    }
}