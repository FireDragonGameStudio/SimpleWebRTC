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
        webRTCConnection.WebSocketConnection.AddListener(OnWebSocketStateChange);
        webRTCConnection.WebRTCConnected.AddListener(OnSignalingComplete);

        SetUIElements(false);
    }

    private void OnDestroy() {
        JoinLobbyButton.onClick.RemoveAllListeners();
        LeaveLobbyButton.onClick.RemoveAllListeners();
        SendChatMessageButton.onClick.RemoveAllListeners();
        StartVideoButton.onClick.RemoveAllListeners();
        StartAudioButton.onClick.RemoveAllListeners();
    }

    private void OnJoinLobby() {
        webRTCConnection.WebSocketConnectionActive = true;
    }

    private void OnLeaveLobby() {
        var message = $"{playerName} left the lobby.";
        Debug.Log(message);
        SendLobbyChatMessage(message);

        if (webRTCConnection.IsWebSocketConnected) {
            webRTCConnection.WebSocketConnectionActive = false;
        }

        SetUIElements(false);
    }

    private void OnWebSocketStateChange(WebSocketState webSocketState) {
        switch (webSocketState) {
            case WebSocketState.Open:
                var message = $"{playerName} joined the lobby.";
                Debug.Log(message);
                SendLobbyChatMessage(message);
                SetUIElements(true);

                break;
            case WebSocketState.Closed:
                Debug.Log($"{playerName} left the lobby and disconnected form WebSocket server.");
                break;
        }
    }

    private void SetUIElements(bool inLobby) {
        JoinLobbyButton.gameObject.SetActive(!inLobby);
        LeaveLobbyButton.gameObject.SetActive(inLobby);
        SendChatMessageButton.gameObject.SetActive(inLobby);
        StartVideoButton.gameObject.SetActive(inLobby);
        StartAudioButton.gameObject.SetActive(inLobby);
        lobbyChatText.gameObject.SetActive(inLobby);
    }

    private void OnSignalingComplete() {
        Debug.Log("WebRTC is now ready. You can start the game now!");
    }

    private void OnStartVideo() {
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

    public void ReceiveLobbyChatMessage(string message) {
        if (webRTCConnection.IsWebSocketConnected && webRTCConnection.IsWebRTCActive) {
            lobbyChatText.text += "\n" + message;
        }
    }
}
