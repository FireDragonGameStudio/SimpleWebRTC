#if VISUAL_SCRIPTING_INSTALLED
using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitCategory("SimpleWebRTC")]
    public class SendWebSocketMessage : Unit {
        [DoNotSerialize, PortLabelHidden]
        public ControlInput InputTrigger { get; private set; }

        [DoNotSerialize, PortLabelHidden]
        public ControlOutput OutputTrigger { get; private set; }

        [DoNotSerialize]
        public ValueInput message;

        protected override void Definition() {
            InputTrigger = ControlInput("InputTrigger", SendMessageViaWebSocket);
            OutputTrigger = ControlOutput("OutputTrigger");

            message = ValueInput<string>("WebSocketMessage", "WebSocket Message");

            Succession(InputTrigger, OutputTrigger);
        }

        private ControlOutput SendMessageViaWebSocket(Flow flow) {
            SimpleWebRTCSceneVariables.ClientConnection.PeerManager?.Signaling?.Send(flow.GetValue<string>(message));

            return OutputTrigger;
        }
    }
}
#endif