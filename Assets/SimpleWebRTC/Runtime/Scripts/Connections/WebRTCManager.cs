using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleWebRTC {
    public class WebRTCManager {

        public event Action<WebSocketState> OnWebSocketConnection;
        public event Action OnWebRTCConnection;
        public event Action<string> OnDataChannelConnection;
        public event Action<string> OnDataChannelMessageReceived;
        public event Action OnVideoStreamEstablished;
        public event Action OnAudioStreamEstablished;

        public bool IsWebSocketConnected { get; private set; }
        public bool IsWebSocketConnectionInProgress { get; private set; }

        private WebSocket ws;
        private bool isLocalPeerVideoAudioSender;
        private bool isLocalPeerVideoAudioReceiver;

        private readonly Dictionary<string, RTCPeerConnection> peerConnections = new Dictionary<string, RTCPeerConnection>();
        private readonly Dictionary<string, RTCDataChannel> senderDataChannels = new Dictionary<string, RTCDataChannel>();
        private readonly Dictionary<string, RTCDataChannel> receiverDataChannels = new Dictionary<string, RTCDataChannel>();
        private readonly Dictionary<string, RTCRtpSender> videoTrackSenders = new Dictionary<string, RTCRtpSender>();
        private readonly Dictionary<string, RTCRtpSender> audioTrackSenders = new Dictionary<string, RTCRtpSender>();

        private readonly Dictionary<string, RawImage> videoReceivers = new Dictionary<string, RawImage>();
        private readonly Dictionary<string, AudioSource> audioReceivers = new Dictionary<string, AudioSource>();

        private readonly string localPeerId;
        private readonly string stunServerAddress;
        private readonly WebRTCConnection connectionGameObject;

        public WebRTCManager(string localPeerId, string stunServerAddress, WebRTCConnection connectionObject) {
            this.localPeerId = localPeerId;
            this.stunServerAddress = stunServerAddress;
            this.connectionGameObject = connectionObject;
        }

        public async void Connect(string webSocketUrl, bool useHTTPHeader, bool isVideoAudioSender, bool isVideoAudioReceiver) {

            IsWebSocketConnectionInProgress = true;
            isLocalPeerVideoAudioSender = isVideoAudioSender;
            isLocalPeerVideoAudioReceiver = isVideoAudioReceiver;

            if (ws == null) {
                // using header data using e.g. glitch.com, or without header using e.g. repl.it
                ws = (useHTTPHeader
                    ? new WebSocket(webSocketUrl, new Dictionary<string, string>() { { "user-agent", "unity webrtc" } })
                    : new WebSocket(webSocketUrl));

                ws.OnOpen += () => {
                    SimpleWebRTCLogger.Log("WebSocket connection opened!");

                    IsWebSocketConnected = true;
                    IsWebSocketConnectionInProgress = false;

                    OnWebSocketConnection?.Invoke(WebSocketState.Open);
                    SendWebSocketMessage(SignalingMessageType.NEWPEER, localPeerId, "ALL", $"New peer {localPeerId}");
                };
                ws.OnMessage += HandleMessage;
                ws.OnError += (e) => SimpleWebRTCLogger.LogError("Error! " + e);
                ws.OnClose += (e) => {
                    SimpleWebRTCLogger.Log("WebSocket connection closed!");
                    IsWebSocketConnected = false;
                    IsWebSocketConnectionInProgress = false;
                    OnWebSocketConnection?.Invoke(WebSocketState.Closed);
                };
            }

            // important for video transmission, to restart webrtc update coroutine
            connectionGameObject.StopCoroutine(WebRTC.Update());
            connectionGameObject.StartCoroutine(WebRTC.Update());

            await ws.Connect();
        }

        private void SetupPeerConnection(string peerId) {
            peerConnections.Add(peerId, CreateNewRTCPeerConnection());
            SetupEventHandlers(peerId);
        }

        private RTCPeerConnection CreateNewRTCPeerConnection() {
            if (string.IsNullOrEmpty(stunServerAddress)) {
                return new RTCPeerConnection();
            }

            RTCConfiguration config = new RTCConfiguration {
                iceServers = new[] {
                    new RTCIceServer { urls = new[] { stunServerAddress } }
                }
            };
            return new RTCPeerConnection(ref config);
        }

        private void SetupEventHandlers(string peerId) {
            peerConnections[peerId].OnIceCandidate = candidate => {
                var candidateInit = new CandidateInit() {
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                    candidate = candidate.Candidate
                };
                SendWebSocketMessage(SignalingMessageType.CANDIDATE, localPeerId, peerId, candidateInit.ConvertToJSON());
            };

            peerConnections[peerId].OnIceConnectionChange = state => {
                SimpleWebRTCLogger.Log($"{localPeerId} connection {peerId} changed to {state}");
                if (state == RTCIceConnectionState.Completed) {
                    connectionGameObject.Connect();

                    // will only be invoked on offering side
                    OnWebRTCConnection?.Invoke();

                    // send completed to other peer of connection too
                    SendWebSocketMessage(SignalingMessageType.COMPLETE, localPeerId, peerId, $"Peerconnection between {localPeerId} and {peerId} completed.");
                }
            };

            senderDataChannels.Add(peerId, peerConnections[peerId].CreateDataChannel(peerId));
            senderDataChannels[peerId].OnOpen = () => SimpleWebRTCLogger.LogDataChannel($"DataChannel {peerId} opened on {localPeerId}.");
            senderDataChannels[peerId].OnMessage = (bytes) => {
                var message = Encoding.UTF8.GetString(bytes);
                SimpleWebRTCLogger.LogDataChannel($"{localPeerId} received on {peerId} senderDataChannel: {message}");
                OnDataChannelMessageReceived?.Invoke(Encoding.UTF8.GetString(bytes));
            };
            senderDataChannels[peerId].OnClose = () => SimpleWebRTCLogger.LogDataChannel($"DataChannel {peerId} closed on {localPeerId}.");
            SimpleWebRTCLogger.LogDataChannel($"SenderDataChannel for {peerId} created on {localPeerId}.");

            peerConnections[peerId].OnDataChannel = channel => {
                receiverDataChannels[peerId] = channel;
                receiverDataChannels[peerId].OnMessage = bytes => {
                    var message = Encoding.UTF8.GetString(bytes);
                    SimpleWebRTCLogger.LogDataChannel($"{localPeerId} received on {peerId} receiverDataChannel: {message}");
                    OnDataChannelMessageReceived?.Invoke(Encoding.UTF8.GetString(bytes));
                };

                SimpleWebRTCLogger.LogDataChannel($"ReceiverDataChannel connection for {peerId} established on {localPeerId}.");

                // peerconnection is now rdy to receive, tell sender side about it and trigger datachannel event
                SendWebSocketMessage(SignalingMessageType.DATA, localPeerId, peerId, $"ReceiverDataChannel on {localPeerId} for {peerId} established.");
            };
            SimpleWebRTCLogger.LogDataChannel($"ReceiverDataChannel for {peerId} created on {localPeerId}.");

            peerConnections[peerId].OnTrack = e => {
                if (e.Track is VideoStreamTrack video) {
                    OnVideoStreamEstablished?.Invoke();

                    video.OnVideoReceived += tex => videoReceivers[peerId].texture = tex;

                    SimpleWebRTCLogger.Log("Receiving video stream.");
                }
                if (e.Track is AudioStreamTrack audio) {
                    OnAudioStreamEstablished?.Invoke();

                    var audioReceiver = audioReceivers[peerId];
                    audioReceiver.SetTrack(audio);
                    audioReceiver.loop = true;
                    audioReceiver.Play();

                    SimpleWebRTCLogger.Log("Receiving audio stream.");
                }
            };

            // not needed, because negotiation is done manually
            // rly?
            peerConnections[peerId].OnNegotiationNeeded = () => {
                if (peerConnections[peerId].SignalingState != RTCSignalingState.Stable) {
                    connectionGameObject.StartCoroutine(CreateOffer());
                }
            };
        }

        private void HandleMessage(byte[] bytes) {
            var data = Encoding.UTF8.GetString(bytes);
            SimpleWebRTCLogger.Log($"Received WebSocket message: {data}");

            var signalingMessage = new SignalingMessage(data);

            switch (signalingMessage.Type) {
                case SignalingMessageType.NEWPEER:

                    // only create receiving resources for remote peers which are going to send multimedia data and receiving local peer
                    if (signalingMessage.IsVideoAudioSender && isLocalPeerVideoAudioReceiver) {
                        CreateNewPeerVideoAudioReceivingResources(signalingMessage.SenderPeerId);
                    }

                    SetupPeerConnection(signalingMessage.SenderPeerId);
                    SimpleWebRTCLogger.Log($"NEWPEER: Created new peerconnection {signalingMessage.SenderPeerId} on peer {localPeerId}");

                    // send ACK to all clients to reach convergence
                    SendWebSocketMessage(SignalingMessageType.NEWPEERACK, localPeerId, "ALL", "New peer ACK", peerConnections.Count, isLocalPeerVideoAudioSender);
                    break;
                case SignalingMessageType.NEWPEERACK:
                    if (!peerConnections.ContainsKey(signalingMessage.SenderPeerId)) {

                        // only create receiving resources for remote peers which are going to send multimedia data and receiving local peer
                        if (signalingMessage.IsVideoAudioSender && isLocalPeerVideoAudioReceiver) {
                            CreateNewPeerVideoAudioReceivingResources(signalingMessage.SenderPeerId);
                        }

                        SetupPeerConnection(signalingMessage.SenderPeerId);
                        SimpleWebRTCLogger.Log($"NEWPEERACK: Created new peerconnection {signalingMessage.SenderPeerId} on peer {localPeerId}");

                        // is every connection updated?
                        if (signalingMessage.ConnectionCount == peerConnections.Count) {
                            connectionGameObject.ConnectWebRTC();
                        }
                    }
                    break;
                case SignalingMessageType.OFFER:
                    if (signalingMessage.ReceiverPeerId.Equals(localPeerId)) {
                        HandleOffer(signalingMessage.SenderPeerId, signalingMessage.Message);
                    }
                    break;
                case SignalingMessageType.ANSWER:
                    if (signalingMessage.ReceiverPeerId.Equals(localPeerId)) {
                        HandleAnswer(signalingMessage.SenderPeerId, signalingMessage.Message);
                    }
                    break;
                case SignalingMessageType.CANDIDATE:
                    if (signalingMessage.ReceiverPeerId.Equals(localPeerId)) {
                        HandleCandidate(signalingMessage.SenderPeerId, signalingMessage.Message);
                    }
                    break;
                case SignalingMessageType.DISPOSE:
                    if (peerConnections.ContainsKey(signalingMessage.SenderPeerId)) {
                        peerConnections[signalingMessage.SenderPeerId].Close();
                        peerConnections.Remove(signalingMessage.SenderPeerId);

                        if (senderDataChannels.ContainsKey(signalingMessage.SenderPeerId)) {
                            senderDataChannels.Remove(signalingMessage.SenderPeerId);
                        }
                        if (receiverDataChannels.ContainsKey(signalingMessage.SenderPeerId)) {
                            receiverDataChannels.Remove(signalingMessage.SenderPeerId);
                        }

                        if (videoTrackSenders.ContainsKey(signalingMessage.SenderPeerId)) {
                            videoTrackSenders.Remove(signalingMessage.SenderPeerId);
                        }
                        if (videoReceivers.ContainsKey(signalingMessage.SenderPeerId)) {
                            GameObject.Destroy(videoReceivers[signalingMessage.SenderPeerId].gameObject);
                            videoReceivers.Remove(signalingMessage.SenderPeerId);
                        }

                        if (audioTrackSenders.ContainsKey(signalingMessage.SenderPeerId)) {
                            audioTrackSenders.Remove(signalingMessage.SenderPeerId);
                        }
                        if (audioReceivers.ContainsKey(signalingMessage.SenderPeerId)) {
                            GameObject.Destroy(audioReceivers[signalingMessage.SenderPeerId].gameObject);
                            audioReceivers.Remove(signalingMessage.SenderPeerId);
                        }

                        SimpleWebRTCLogger.Log($"DISPOSE: Peerconnection for {signalingMessage.SenderPeerId} removed on peer {localPeerId}");
                    }
                    break;
                case SignalingMessageType.DATA:
                    if (localPeerId.Equals(signalingMessage.ReceiverPeerId) && senderDataChannels[signalingMessage.SenderPeerId].ReadyState == RTCDataChannelState.Open) {
                        OnDataChannelConnection?.Invoke(signalingMessage.SenderPeerId);
                    }
                    break;
                case SignalingMessageType.COMPLETE:
                    if (localPeerId.Equals(signalingMessage.ReceiverPeerId)) {
                        connectionGameObject.ConnectWebRTC();

                        // invoke complete on answering side
                        OnWebRTCConnection?.Invoke();
                    }
                    break;
                default:
                    SimpleWebRTCLogger.Log($"Received NOTYPE from {signalingMessage.SenderPeerId} : {data}");
                    break;
            }
        }

        private void CreateNewPeerVideoAudioReceivingResources(string senderPeerId) {
            // create new video receiver gameobject
            var receivingRawImage = new GameObject().AddComponent<RawImage>();
            receivingRawImage.name = $"{senderPeerId}-Receiving-RawImage";
            receivingRawImage.rectTransform.SetParent(connectionGameObject.ReceivingRawImagesParent, false);
            receivingRawImage.rectTransform.localScale = Vector3.one;
            receivingRawImage.rectTransform.anchorMin = Vector2.zero;
            receivingRawImage.rectTransform.anchorMax = Vector2.one;
            receivingRawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            receivingRawImage.rectTransform.sizeDelta = Vector2.zero;
            videoReceivers[senderPeerId] = receivingRawImage;

            // create new audio receiver gameobject
            var receivingAudioSource = new GameObject().AddComponent<AudioSource>();
            receivingAudioSource.name = $"{senderPeerId}-Receiving-AudioSource";
            receivingAudioSource.transform.SetParent(connectionGameObject.ReceivingAudioSourceParent);
            audioReceivers[senderPeerId] = receivingAudioSource;

            // refresh layout group for proper display - not needed i guess
            //var parentGroupLayout = connectionGameObject.ReceivingRawImagesParent.GetComponent<LayoutGroup>();
            //LayoutRebuilder.ForceRebuildLayoutImmediate(parentGroupLayout.GetComponent<RectTransform>());
        }

        private IEnumerator CreateOffer() {
            foreach (var peerConnection in peerConnections) {

                // enforce unified codec profiles
                var transceivers = peerConnection.Value.GetTransceivers();
                foreach (var transceiver in transceivers) {
                    if (transceiver.Sender != null && transceiver.Sender?.Track?.Kind == TrackKind.Video) {
                        var vp8 = RTCRtpSender.GetCapabilities(TrackKind.Video).codecs.Where(c => c.mimeType == "video/VP8").ToArray();
                        transceiver.SetCodecPreferences(vp8);
                    }
                }

                var offer = peerConnection.Value.CreateOffer();
                yield return offer;

                if (!offer.IsError) {
                    var offerDesc = offer.Desc;
                    var localDescOp = peerConnection.Value.SetLocalDescription(ref offerDesc);
                    yield return localDescOp;

                    var offerSessionDesc = new SessionDescription {
                        type = peerConnection.Value.LocalDescription.type.ToString().ToLower(),
                        sdp = peerConnection.Value.LocalDescription.sdp
                    };
                    SendWebSocketMessage(SignalingMessageType.OFFER, localPeerId, peerConnection.Key, offerSessionDesc.ConvertToJSON());
                } else {
                    Debug.LogError($"{localPeerId} - Failed create offer for {peerConnection.Key}. {offer.Error.message}");
                }
            }
        }

        private void HandleOffer(string senderPeerId, string offerJson) {
            SimpleWebRTCLogger.Log($"{localPeerId} got OFFER from {senderPeerId} : {offerJson}");
            connectionGameObject.StartCoroutine(CreateAnswer(senderPeerId, offerJson));
        }

        private IEnumerator CreateAnswer(string senderPeerId, string offerJson) {

            var receivedOfferSessionDesc = SessionDescription.FromJSON(offerJson);

            // Only use VP8 codecs before setting remote description
            string sdp = receivedOfferSessionDesc.StripNonVP8CodecsFromSdp();

            var offerSessionDesc = new RTCSessionDescription {
                type = RTCSdpType.Offer,
                sdp = sdp
            };

            var remoteDescOp = peerConnections[senderPeerId].SetRemoteDescription(ref offerSessionDesc);
            yield return remoteDescOp;

            if (peerConnections[senderPeerId].RemoteDescription.Equals(default(RTCSessionDescription)) ||
                peerConnections[senderPeerId].RemoteDescription.type != RTCSdpType.Offer) {
                Debug.LogError($"{localPeerId} - Failed to set remote description for {senderPeerId}");
                yield break;
            }

            var answer = peerConnections[senderPeerId].CreateAnswer();
            yield return answer;

            var answerDesc = answer.Desc;

            if (answerDesc.type != RTCSdpType.Answer || string.IsNullOrEmpty(answerDesc.sdp)) {
                Debug.LogWarning($"{localPeerId} has no answer sdp for {senderPeerId}! ANSWER TYPE: {answer.GetType().ToString()} ANSWERDESC TYPE: {answerDesc.type} ANSWERDESC: {answerDesc.ToString()}");
                yield break;
            }

            var localDescOp = peerConnections[senderPeerId].SetLocalDescription(ref answerDesc);
            yield return localDescOp;

            var answerSessionDesc = new SessionDescription {
                type = answerDesc.type.ToString().ToLower(),
                sdp = answerDesc.sdp
            };
            SendWebSocketMessage(SignalingMessageType.ANSWER, localPeerId, senderPeerId, answerSessionDesc.ConvertToJSON());
        }

        private void HandleAnswer(string senderPeerId, string answerJson) {

            SimpleWebRTCLogger.Log($"{localPeerId} got ANSWER from {senderPeerId} : {answerJson}");

            var receivedAnswerSessionDesc = SessionDescription.FromJSON(answerJson);
            RTCSessionDescription answerSessionDesc = new RTCSessionDescription() {
                type = RTCSdpType.Answer,
                sdp = receivedAnswerSessionDesc.sdp
            };
            peerConnections[senderPeerId].SetRemoteDescription(ref answerSessionDesc);
        }

        private void HandleCandidate(string senderPeerId, string candidateJson) {

            SimpleWebRTCLogger.Log($"{localPeerId} got CANDIDATE from {senderPeerId} : {candidateJson}");

            var candidateInit = CandidateInit.FromJSON(candidateJson);

            if (string.IsNullOrEmpty(candidateInit.candidate)) {
                SimpleWebRTCLogger.Log($"{localPeerId} got CANDIDATE GATHERING END from {senderPeerId}.");
                return;
            }

            RTCIceCandidateInit init = new RTCIceCandidateInit() {
                sdpMid = candidateInit.sdpMid,
                sdpMLineIndex = candidateInit.sdpMLineIndex,
                candidate = candidateInit.candidate
            };
            var candidate = new RTCIceCandidate(init);
            peerConnections[senderPeerId].AddIceCandidate(candidate);
        }

        public void CloseWebRTC() {
            connectionGameObject.StopAllCoroutines();

            foreach (var senderDataChannel in senderDataChannels) {
                senderDataChannel.Value.Close();
            }
            foreach (var receiverDataChannel in receiverDataChannels) {
                receiverDataChannel.Value.Close();
            }

            foreach (var videoTrackSender in videoTrackSenders) {
                videoTrackSender.Value.Dispose();
            }
            foreach (var audioTrackSender in audioTrackSenders) {
                audioTrackSender.Value.Dispose();
            }

            foreach (var peerConnection in peerConnections) {
                peerConnection.Value.Close();
            }

            // remove peer connections for localPeer on other peers
            SendWebSocketMessage(SignalingMessageType.DISPOSE, localPeerId, "ALL", $"Remove peerConnection for {localPeerId}.");

            peerConnections.Clear();

            senderDataChannels.Clear();
            receiverDataChannels.Clear();

            videoTrackSenders.Clear();
            foreach (var videoReceiver in videoReceivers) {
                if (videoReceiver.Value != null) {
                    GameObject.Destroy(videoReceiver.Value.gameObject);
                }
            }
            videoReceivers.Clear();

            audioTrackSenders.Clear();
            foreach (var audioReceiver in audioReceivers) {
                if (audioReceiver.Value != null) {
                    GameObject.Destroy(audioReceiver.Value.gameObject);
                }
            }
            audioReceivers.Clear();
        }

        public async void CloseWebSocket() {
            if (ws != null) {
                await ws.Close();

                // reset manually, because ws is not reusable after closing
                ws = null;
            }
        }

        public void InstantiateWebRTC() {
            connectionGameObject.StartCoroutine(CreateOffer());
        }

        public void DispatchMessageQueue() {
            ws?.DispatchMessageQueue();
        }

        public void SendViaDataChannel(string message) {
            foreach (var senderDataChannel in senderDataChannels) {
                senderDataChannel.Value?.Send(message);
            }
        }

        public void SendViaDataChannel(string targetPeerId, string message) {
            senderDataChannels[targetPeerId]?.Send(message);
        }

        public void AddVideoTrack(VideoStreamTrack videoStreamTrack) {

            // optional video stream preview
            if (connectionGameObject.OptionalPreviewRawImage != null) {
                connectionGameObject.OptionalPreviewRawImage.texture = videoStreamTrack.Texture;
            }

            foreach (var peerConnection in peerConnections) {
                videoTrackSenders.Add(peerConnection.Key, peerConnection.Value.AddTrack(videoStreamTrack));
            }
            connectionGameObject.StartCoroutine(CreateOffer());
        }

        public void RemoveVideoTrack() {
            foreach (var peerConnection in peerConnections) {
                if (videoTrackSenders.ContainsKey(peerConnection.Key)) {
                    peerConnection.Value.RemoveTrack(videoTrackSenders[peerConnection.Key]);
                    videoTrackSenders.Remove(peerConnection.Key);
                }
            }
            // reset optional video stream preview
            if (connectionGameObject.OptionalPreviewRawImage != null) {
                connectionGameObject.OptionalPreviewRawImage.texture = null;
            }
        }

        public void AddAudioTrack(AudioStreamTrack audioStreamTrack) {
            foreach (var peerConnection in peerConnections) {
                audioTrackSenders.Add(peerConnection.Key, peerConnection.Value.AddTrack(audioStreamTrack));
            }
            connectionGameObject.StartCoroutine(CreateOffer());
        }

        public void RemoveAudioTrack() {
            foreach (var peerConnection in peerConnections) {
                if (audioTrackSenders.ContainsKey(peerConnection.Key)) {
                    peerConnection.Value.RemoveTrack(audioTrackSenders[peerConnection.Key]);
                    audioTrackSenders.Remove(peerConnection.Key);
                }
            }
        }

        public void SendWebSocketTestMessage(string message) {
            ws?.SendText(message);
        }

        public void SendWebSocketMessage(SignalingMessageType messageType, string senderPeerId, string receiverPeerId, string message) {
            SendWebSocketMessage(messageType, senderPeerId, receiverPeerId, message, peerConnections.Count, isLocalPeerVideoAudioSender);
        }

        public void SendWebSocketMessage(SignalingMessageType messageType, string senderPeerId, string receiverPeerId, string message, int connectionCount, bool isVideoAudioSender) {
            ws?.SendText($"{Enum.GetName(typeof(SignalingMessageType), messageType)}|{senderPeerId}|{receiverPeerId}|{message}|{connectionCount}|{isVideoAudioSender}");
        }
    }
}