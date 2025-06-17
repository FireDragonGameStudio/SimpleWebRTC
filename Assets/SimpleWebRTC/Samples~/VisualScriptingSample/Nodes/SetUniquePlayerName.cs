using Unity.VisualScripting;

namespace SimpleWebRTC {
    [UnitCategory("SimpleWebRTC")]
    public class SetUniquePlayerName : Unit {
        [DoNotSerialize, PortLabelHidden]
        public ControlInput InputTrigger { get; private set; }

        [DoNotSerialize, PortLabelHidden]
        public ControlOutput OutputTrigger { get; private set; }

        [DoNotSerialize]
        public ValueInput playerName;

        protected override void Definition() {
            InputTrigger = ControlInput("InputTrigger", SetUniquePlayerNameValue);
            OutputTrigger = ControlOutput("OutputTrigger");

            playerName = ValueInput<string>("PlayerName", "Unique Player Name");

            Succession(InputTrigger, OutputTrigger);
        }

        private ControlOutput SetUniquePlayerNameValue(Flow flow) {
            SimpleWebRTCSceneVariables.ClientConnection.SetUniquePlayerName(flow.GetValue<string>(playerName));

            return OutputTrigger;
        }
    }
}
