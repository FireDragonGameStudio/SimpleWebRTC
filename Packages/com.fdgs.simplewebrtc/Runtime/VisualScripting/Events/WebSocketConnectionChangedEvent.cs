#if VISUAL_SCRIPTING_INSTALLED
using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnSignalingStateChanged")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class WebSocketConnectionChangedEvent : EventUnit<SignalingState> {

        [DoNotSerialize]
        public ValueOutput result { get; private set; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.WebSocketConnectionChanged);
        }

        protected override void Definition() {
            base.Definition();

            result = ValueOutput<SignalingState>(nameof(result));
        }

        protected override void AssignArguments(Flow flow, SignalingState data) {
            flow.SetValue(result, data);
        }

        public static void Trigger(SignalingState eventData) {
            EventBus.Trigger(SimpleWebRTCEventNames.WebSocketConnectionChanged, eventData);
        }
    }
}
#endif