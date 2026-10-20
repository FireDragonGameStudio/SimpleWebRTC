using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// Sends audio to all peers, either from an AudioSource (e.g. music or game sounds) or from a microphone.
    /// Remember to request microphone permission on mobile platforms before streaming.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Audio/Audio Sender")]
    public class AudioSender : PeerSessionComponent {

        [SerializeField] private AudioSource audioSource;
        [Tooltip("Record the microphone into the AudioSource instead of sending its clip.")]
        [SerializeField] private bool useMicrophone = false;
        [Tooltip("Leave empty to use the default microphone.")]
        [SerializeField] private string microphoneDevice = "";
        [Tooltip("Also play the sent audio locally. Usually off for microphones to avoid echo.")]
        [SerializeField] private bool playLocally = true;
        [Tooltip("Activates the AudioSource GameObject only while streaming.")]
        [SerializeField] private bool activateSourceWhileStreaming = true;
        [SerializeField] private bool streamOnEnable = false;

        [Header("Events")]
        public UnityEvent StreamingStarted = new UnityEvent();
        public UnityEvent StreamingStopped = new UnityEvent();

        private readonly Dictionary<string, RTCRtpTransceiver> transceivers = new Dictionary<string, RTCRtpTransceiver>();
        private AudioStreamTrack track;
        private Coroutine startRoutine;
        private AudioClip clipBeforeMicrophone;

        public AudioSource AudioSource {
            get => audioSource;
            set => audioSource = value;
        }

        public bool UseMicrophone {
            get => useMicrophone;
            set => useMicrophone = value;
        }

        public bool IsStreaming => track != null;

        protected override void Reset() {
            base.Reset();
            audioSource = GetComponentInChildren<AudioSource>();
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (streamOnEnable) {
                StartStreaming();
            }
        }

        protected override void OnDisable() {
            StopStreaming();
            base.OnDisable();
        }

        [InspectorButton]
        public void StartStreaming() {
            if (IsStreaming || startRoutine != null) {
                return;
            }
            if (audioSource == null) {
                SimpleWebRTCLogger.LogError($"{name}: no AudioSource assigned to {nameof(AudioSender)}.");
                return;
            }
            startRoutine = StartCoroutine(StartStreamingRoutine());
        }

        [InspectorButton]
        public void StopStreaming() {
            if (startRoutine != null) {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }
            if (!IsStreaming) {
                return;
            }
            foreach (var transceiver in transceivers.Values) {
                transceiver.Sender.ReplaceTrack(null);
                transceiver.Direction = RTCRtpTransceiverDirection.Inactive;
            }
            track.Stop();
            track.Dispose();
            track = null;

            if (audioSource != null) {
                audioSource.Stop();
                if (useMicrophone) {
                    Microphone.End(microphoneDevice);
                    audioSource.clip = clipBeforeMicrophone;
                }
                if (activateSourceWhileStreaming) {
                    audioSource.gameObject.SetActive(false);
                }
            }
            SimpleWebRTCLogger.Log($"{name}: audio streaming stopped.");
            StreamingStopped?.Invoke();
        }

        public void ToggleStreaming() {
            if (IsStreaming) {
                StopStreaming();
            } else {
                StartStreaming();
            }
        }

        private IEnumerator StartStreamingRoutine() {
            if (activateSourceWhileStreaming) {
                audioSource.gameObject.SetActive(true);
            }

            if (useMicrophone) {
                if (Microphone.devices.Length == 0) {
                    SimpleWebRTCLogger.LogError($"{name}: no microphone found.");
                    startRoutine = null;
                    yield break;
                }
                clipBeforeMicrophone = audioSource.clip;
                audioSource.clip = Microphone.Start(microphoneDevice, true, 1, AudioSettings.outputSampleRate);
                audioSource.loop = true;
                yield return new WaitUntil(() => Microphone.GetPosition(microphoneDevice) > 0);
            }

            audioSource.Play();
            track = new AudioStreamTrack(audioSource) { Loopback = playLocally };
            startRoutine = null;

            foreach (var transceiver in transceivers.Values) {
                transceiver.Sender.ReplaceTrack(track);
                transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;
            }
            SimpleWebRTCLogger.Log($"{name}: audio streaming started.");
            StreamingStarted?.Invoke();
        }

        protected override void OnSessionCreated(PeerSession session) {
            if (transceivers.ContainsKey(session.RemotePeerId)) {
                return;
            }
            transceivers[session.RemotePeerId] = session.AddSendTransceiver(TrackKind.Audio, track);
        }

        protected override void OnSessionClosed(PeerSession session) {
            transceivers.Remove(session.RemotePeerId);
        }
    }
}
