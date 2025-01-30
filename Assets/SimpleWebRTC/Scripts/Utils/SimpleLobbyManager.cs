using NativeWebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleLobbyManager : MonoBehaviour {

    [SerializeField] private Button JoinLobbyButton;
    [SerializeField] private Button LeaveLobbyButton;
    [SerializeField] private Button SendChatMessageButton;
    [SerializeField] private Button StartVideoButton;
    [SerializeField] private Button StartAudioButton;
    [SerializeField] private TextMeshProUGUI lobbyChatText;

    [Header("Player Data")]
    [SerializeField] private string playerName = "Player ";

    [Header("WebRTC Connection")]
    [SerializeField] private WebRTCConnection webRTCConnection;

    private void Awake() {
        playerName = playerName + SystemInfo.deviceName;
        webRTCConnection.LocalPeerId = playerName;

        // register events for lobby
        JoinLobbyButton.onClick.AddListener(OnJoinLobby);
        LeaveLobbyButton.onClick.AddListener(OnLeaveLobby);
        SendChatMessageButton.onClick.AddListener(OnSendLobbyChatMessage);
        StartVideoButton.onClick.AddListener(OnStartVideo);
        StartAudioButton.onClick.AddListener(OnStartAudio);

        // register events for connection
        webRTCConnection.WebSocketConnected.AddListener(OnWebSocketConnected);
        webRTCConnection.WebRTCConnected.AddListener(OnSignalingComplete);
        webRTCConnection.DataChannelConnected.AddListener(OnDataChannelConnected);

        SetUIElements(false);
    }

    private void OnDestroy() {
        JoinLobbyButton.onClick.RemoveAllListeners();
        LeaveLobbyButton.onClick.RemoveAllListeners();
        SendChatMessageButton.onClick.RemoveAllListeners();
        StartVideoButton.onClick.RemoveAllListeners();
        StartAudioButton.onClick.RemoveAllListeners();

        // de-register events for connection
        webRTCConnection.WebSocketConnected.RemoveListener(OnWebSocketConnected);
        webRTCConnection.WebRTCConnected.RemoveListener(OnSignalingComplete);
        webRTCConnection.DataChannelConnected.RemoveListener(OnDataChannelConnected);
    }

    private void OnJoinLobby() {
        webRTCConnection.gameObject.SetActive(true);
    }

    private void OnLeaveLobby() {
        var message = $"{playerName} left the lobby.";
        Debug.Log(message);
        SendLobbyChatMessage(message);

        webRTCConnection.gameObject.SetActive(false);

        SetUIElements(false);
    }

    private void SetUIElements(bool inLobby) {
        JoinLobbyButton.gameObject.SetActive(!inLobby);
        LeaveLobbyButton.gameObject.SetActive(inLobby);
        SendChatMessageButton.gameObject.SetActive(inLobby);
        StartVideoButton.gameObject.SetActive(inLobby);
        StartAudioButton.gameObject.SetActive(inLobby);
        lobbyChatText.gameObject.SetActive(inLobby);
    }

    private void OnWebSocketConnected(WebSocketState state) {
        Debug.Log($"WebSocket connection state is: {state}");

        if (state == WebSocketState.Open) {
            JoinLobbyButton.gameObject.SetActive(false);
            LeaveLobbyButton.gameObject.SetActive(true);
        }
    }

    private void OnSignalingComplete() {
        Debug.Log("WebRTC is now ready. You can start the game now!");
        SetUIElements(true);
    }

    private void OnDataChannelConnected(string peerId) {
        var message = $"{playerName} can now send messages.";
        Debug.Log(message);
        SendLobbyChatMessageToPlayer(peerId, message);
    }

    private void OnStartVideo() {
        webRTCConnection.RestartVideoTransmission();
        webRTCConnection.StartStopVideoTransmission = true;
    }

    private void OnStartAudio() {
        webRTCConnection.StartStopAudioChannel = true;
    }

    private void OnSendLobbyChatMessage() {
        SendLobbyChatMessage($"{playerName}: PLS SEND ME YOUR VIDEO & AUDIO");
    }

    private void SendLobbyChatMessage(string message) {
        if (webRTCConnection.IsWebSocketConnected && webRTCConnection.IsWebRTCActive) {
            lobbyChatText.text += "\n" + message;
            webRTCConnection.SendDataChannelMessage(message);
        }
    }

    private void SendLobbyChatMessageToPlayer(string targetPeerId, string message) {
        if (webRTCConnection.IsWebSocketConnected && webRTCConnection.IsWebRTCActive) {
            lobbyChatText.text += "\n" + message;
            webRTCConnection.SendDataChannelMessageToPeer(targetPeerId, message);
        }
    }

    public void ReceiveLobbyChatMessage(string message) {
        if (webRTCConnection.IsWebSocketConnected && webRTCConnection.IsWebRTCActive) {
            lobbyChatText.text += "\n" + message;
        }
    }
}
