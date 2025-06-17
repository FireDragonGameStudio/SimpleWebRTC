using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnDataChannelMessageReceived")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class DataChannelMessageReceivedEvent : EventUnit<string> {

        [DoNotSerialize]
        public ValueOutput message { get; private set; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.DataChannelMessageReceived);
        }

        protected override void Definition() {
            base.Definition();

            message = ValueOutput<string>(nameof(message));
        }

        protected override void AssignArguments(Flow flow, string data) {
            flow.SetValue(message, data);
        }

        public static void Trigger(string eventData) {
            EventBus.Trigger(SimpleWebRTCEventNames.DataChannelMessageReceived, eventData);
        }
    }
}