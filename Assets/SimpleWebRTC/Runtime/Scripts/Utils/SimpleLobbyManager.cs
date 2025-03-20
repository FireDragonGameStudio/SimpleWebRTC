using NativeWebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class SimpleLobbyManager : MonoBehaviour {

        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Button sendChatMessageButton;
        [SerializeField] private Button startVideoButton;
        [SerializeField] private Button stopVideoButton;
        [SerializeField] private Button startAudioButton;
        [SerializeField] private Button stopAudioButton;
        [SerializeField] private TextMeshProUGUI lobbyChatText;

        [Header("Player Data")]
        [SerializeField] private string playerName = "Player ";

        [Header("WebRTC Connection")]
        [SerializeField] private WebRTCConnection webRTCConnection;

        private void Awake() {
            playerName = playerName + SystemInfo.deviceName;
            webRTCConnection.SetUniquePlayerName(playerName);

            // register events for lobby
            joinLobbyButton.onClick.AddListener(OnJoinLobby);
            leaveLobbyButton.onClick.AddListener(OnLeaveLobby);
            sendChatMessageButton.onClick.AddListener(OnSendLobbyChatMessage);
            startVideoButton.onClick.AddListener(OnStartVideo);
            stopVideoButton.onClick.AddListener(OnStopVideo);
            startAudioButton.onClick.AddListener(OnStartAudio);
            stopAudioButton.onClick.AddListener(OnStopAudio);

            // register events for connection
            webRTCConnection.WebSocketConnectionChanged.AddListener(OnJoinedLobby);
            webRTCConnection.WebRTCConnected.AddListener(OnLobbySetupComplete);
            webRTCConnection.DataChannelConnected.AddListener(OnLobbyChatConnected);

            SetUIElements(false);
        }

        private void OnDestroy() {
            joinLobbyButton.onClick.RemoveAllListeners();
            leaveLobbyButton.onClick.RemoveAllListeners();
            sendChatMessageButton.onClick.RemoveAllListeners();
            startVideoButton.onClick.RemoveAllListeners();
            stopVideoButton.onClick.RemoveAllListeners();
            startAudioButton.onClick.RemoveAllListeners();
            stopAudioButton.onClick.RemoveAllListeners();

            // de-register events for connection
            webRTCConnection.WebSocketConnectionChanged.RemoveListener(OnJoinedLobby);
            webRTCConnection.WebRTCConnected.RemoveListener(OnLobbySetupComplete);
            webRTCConnection.DataChannelConnected.RemoveListener(OnLobbyChatConnected);
        }

        private void OnJoinLobby() {
            lobbyChatText.gameObject.SetActive(true);
            lobbyChatText.text = "Connecting...";

            // start client connection
            webRTCConnection.Connect();
        }

        private void OnJoinedLobby(WebSocketState state) {
            Debug.Log($"WebSocket connection state is: {state}");

            if (state == WebSocketState.Open) {
                joinLobbyButton.gameObject.SetActive(false);
                leaveLobbyButton.gameObject.SetActive(true);
                SetUIElements(true);
            }
            if (state == WebSocketState.Closed) {
                lobbyChatText.text = $"{playerName} (you) disconnected from Lobby.";
            }
        }

        private void OnLobbySetupComplete() {
            Debug.Log("WebRTC is now ready. You can start the game now!");
        }

        private void OnLobbyChatConnected(string peerId) {
            var message = $"{playerName} can now send messages.";
            Debug.Log(message);
            SendLobbyChatMessageToPlayer(peerId, message);
        }

        private void OnLeaveLobby() {
            var message = $"{playerName} left the lobby.";
            Debug.Log(message);
            SendLobbyChatMessage(message);

            // disconnect client
            webRTCConnection.Disconnect();

            SetUIElements(false);
        }

        private void OnStartVideo() {
            webRTCConnection.StartVideoTransmission();
        }

        private void OnStopVideo() {
            webRTCConnection.StopVideoTransmission();
        }

        private void OnStartAudio() {
            webRTCConnection.StartAudioTransmission();
        }

        private void OnStopAudio() {
            webRTCConnection.StopAudioTransmission();
        }

        private void OnSendLobbyChatMessage() {
            SendLobbyChatMessage($"{playerName}: PLS SEND ME YOUR VIDEO & AUDIO");
        }

        private void SendLobbyChatMessage(string message) {
            lobbyChatText.text += "\n" + message;
            webRTCConnection.SendDataChannelMessage(message);
        }

        private void SendLobbyChatMessageToPlayer(string targetPeerId, string message) {
            lobbyChatText.text += "\n" + message;
            webRTCConnection.SendDataChannelMessageToPeer(targetPeerId, message);
        }

        public void ReceiveLobbyChatMessage(string message) {
            lobbyChatText.text += "\n" + message;
        }

        private void SetUIElements(bool inLobby) {
            joinLobbyButton.gameObject.SetActive(!inLobby);
            leaveLobbyButton.gameObject.SetActive(inLobby);
            sendChatMessageButton.gameObject.SetActive(inLobby);
            startVideoButton.gameObject.SetActive(inLobby);
            stopVideoButton.gameObject.SetActive(inLobby);
            startAudioButton.gameObject.SetActive(inLobby);
            stopAudioButton.gameObject.SetActive(inLobby);
            lobbyChatText.gameObject.SetActive(inLobby);
        }
    }
}