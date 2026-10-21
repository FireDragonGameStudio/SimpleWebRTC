using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC {
    /// <summary>
    /// Sample: a minimal lobby with a chat over a data channel and buttons to start/stop video and audio.
    /// </summary>
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

        [Header("WebRTC")]
        [SerializeField] private WebRTCPeerManager peerManager;
        [SerializeField] private DataChannel chatChannel;
        [SerializeField] private VideoSender videoSender;
        [SerializeField] private AudioSender audioSender;

        private void Awake() {
            playerName += SystemInfo.deviceName;
            peerManager.SetLocalPeerId(playerName);

            joinLobbyButton.onClick.AddListener(OnJoinLobby);
            leaveLobbyButton.onClick.AddListener(OnLeaveLobby);
            sendChatMessageButton.onClick.AddListener(OnSendLobbyChatMessage);
            startVideoButton.onClick.AddListener(videoSender.StartStreaming);
            stopVideoButton.onClick.AddListener(videoSender.StopStreaming);
            startAudioButton.onClick.AddListener(audioSender.StartStreaming);
            stopAudioButton.onClick.AddListener(audioSender.StopStreaming);

            peerManager.SignalingStateChanged.AddListener(OnSignalingStateChanged);
            peerManager.PeerConnected.AddListener(OnPeerConnected);
            chatChannel.ChannelOpened.AddListener(OnLobbyChatConnected);
            chatChannel.MessageReceived.AddListener(ReceiveLobbyChatMessage);

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

            peerManager.SignalingStateChanged.RemoveListener(OnSignalingStateChanged);
            peerManager.PeerConnected.RemoveListener(OnPeerConnected);
            chatChannel.ChannelOpened.RemoveListener(OnLobbyChatConnected);
            chatChannel.MessageReceived.RemoveListener(ReceiveLobbyChatMessage);
        }

        private void OnJoinLobby() {
            lobbyChatText.gameObject.SetActive(true);
            lobbyChatText.text = "Connecting...";
            peerManager.Connect();
        }

        private void OnSignalingStateChanged(SignalingState state) {
            if (state == SignalingState.Connected) {
                lobbyChatText.text = $"{peerManager.LocalPeerId} (you) joined the lobby.";
                SetUIElements(true);
            }
            if (state == SignalingState.Disconnected) {
                lobbyChatText.text += $"\n{peerManager.LocalPeerId} (you) disconnected from the lobby.";
                SetUIElements(false);
            }
        }

        private void OnPeerConnected(string peerId) {
            Debug.Log($"WebRTC connection to {peerId} is ready.");
        }

        private void OnLobbyChatConnected(string peerId) {
            SendLobbyChatMessageToPlayer(peerId, $"{playerName} can now send messages.");
        }

        private void OnLeaveLobby() {
            SendLobbyChatMessage($"{playerName} left the lobby.");
            peerManager.Disconnect();
        }

        private void OnSendLobbyChatMessage() {
            SendLobbyChatMessage($"{playerName}: PLS SEND ME YOUR VIDEO & AUDIO");
        }

        private void SendLobbyChatMessage(string message) {
            lobbyChatText.text += "\n" + message;
            chatChannel.Send(message);
        }

        private void SendLobbyChatMessageToPlayer(string targetPeerId, string message) {
            lobbyChatText.text += "\n" + message;
            chatChannel.SendTo(targetPeerId, message);
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
            lobbyChatText.gameObject.SetActive(true);
        }
    }
}
