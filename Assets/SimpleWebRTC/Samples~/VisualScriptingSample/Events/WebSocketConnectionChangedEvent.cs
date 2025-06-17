using NativeWebSocket;
using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitTitle("OnWebSocketConnectionChangedEvent")]
    [UnitCategory("Events\\SimpleWebRTC")]
    public class WebSocketConnectionChangedEvent : EventUnit<WebSocketState> {

        [DoNotSerialize]
        public ValueOutput result { get; private set; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) {
            return new EventHook(SimpleWebRTCEventNames.WebSocketConnectionChanged);
        }

        protected override void Definition() {
            base.Definition();

            result = ValueOutput<WebSocketState>(nameof(result));
        }

        protected override void AssignArguments(Flow flow, WebSocketState data) {
            flow.SetValue(result, data);
        }

        public static void Trigger(WebSocketState eventData) {
            EventBus.Trigger(SimpleWebRTCEventNames.WebSocketConnectionChanged, eventData);
        }
    }
}