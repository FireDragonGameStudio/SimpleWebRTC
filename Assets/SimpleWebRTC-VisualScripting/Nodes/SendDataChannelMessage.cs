using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitCategory("SimpleWebRTC")]
    public class SendDataChannelMessage : Unit {
        [DoNotSerialize, PortLabelHidden]
        public ControlInput InputTrigger { get; private set; }

        [DoNotSerialize, PortLabelHidden]
        public ControlOutput OutputTrigger { get; private set; }

        [DoNotSerialize]
        public ValueInput message;

        protected override void Definition() {
            InputTrigger = ControlInput("InputTrigger", SendMessageViaDataChannel);
            OutputTrigger = ControlOutput("OutputTrigger");

            message = ValueInput<string>("DataChannelMessage", "DataChannel Message");

            Succession(InputTrigger, OutputTrigger);
        }

        private ControlOutput SendMessageViaDataChannel(Flow flow) {
            SimpleWebRTCSceneVariables.ClientConnection.SendDataChannelMessage(flow.GetValue<string>(message));

            return OutputTrigger;
        }
    }
}
