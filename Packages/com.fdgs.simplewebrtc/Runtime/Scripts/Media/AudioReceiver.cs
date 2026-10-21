using System;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleWebRTC {
    /// <summary>
    /// Plays the audio of every peer through its own AudioSource, spawned under a parent. Use an AudioSource
    /// prefab to configure spatial audio, mixer groups or volume.
    /// </summary>
    [AddComponentMenu("SimpleWebRTC/Audio/Audio Receiver")]
    public class AudioReceiver : PeerSessionComponent {

        [SerializeField] private Transform spawnParent;
        [Tooltip("Optional template for the spawned AudioSources.")]
        [SerializeField] private AudioSource audioSourcePrefab;

        [Header("Events")]
        [Tooltip("A remote peer started sending audio. Parameter: remote peer id.")]
        public UnityEvent<string> AudioStarted = new UnityEvent<string>();
        [Tooltip("A remote peer stopped sending audio or left. Parameter: remote peer id.")]
        public UnityEvent<string> AudioStopped = new UnityEvent<string>();

        /// <summary>Raised when the AudioSource for a peer starts playing: remote peer id, AudioSource.</summary>
        public event Action<string, AudioSource> AudioSourceStarted;

        private class Entry {
            public string PeerId;
            public RTCRtpTransceiver Transceiver;
            public AudioSource Source;
            public bool IsPlaying;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private readonly List<Entry> changedEntries = new List<Entry>();

        public bool TryGetAudioSource(string remotePeerId, out AudioSource source) {
            source = entries.Values.FirstOrDefault(e => e.PeerId == remotePeerId)?.Source;
            return source != null;
        }

        protected override void OnSessionCreated(PeerSession session) {
            session.TrackReceived += OnTrackReceived;
        }

        protected override void OnSessionClosed(PeerSession session) {
            session.TrackReceived -= OnTrackReceived;
            foreach (var pair in entries.Where(p => p.Value.PeerId == session.RemotePeerId).ToArray()) {
                Stop(pair.Value);
                if (pair.Value.Source != null) {
                    Destroy(pair.Value.Source.gameObject);
                }
                entries.Remove(pair.Key);
            }
        }

        private void OnTrackReceived(PeerSession session, RTCTrackEvent e) {
            if (!(e.Track is AudioStreamTrack audioTrack)) {
                return;
            }
            var key = $"{session.RemotePeerId}/{e.Transceiver.Mid ?? audioTrack.Id}";
            if (!entries.TryGetValue(key, out var entry)) {
                entry = new Entry {
                    PeerId = session.RemotePeerId,
                    Transceiver = e.Transceiver,
                    Source = CreateAudioSource(session.RemotePeerId)
                };
                entries[key] = entry;
            }
            // playback starts in Update, once the negotiation finished and the transceiver is receiving
            entry.Source.SetTrack(audioTrack);
        }

        private void Update() {
            // a stopped sender sets its transceiver to inactive
            changedEntries.Clear();
            foreach (var entry in entries.Values) {
                var direction = entry.Transceiver.CurrentDirection;
                var receiving = direction == RTCRtpTransceiverDirection.RecvOnly || direction == RTCRtpTransceiverDirection.SendRecv;
                if (entry.IsPlaying && !receiving || !entry.IsPlaying && receiving) {
                    changedEntries.Add(entry);
                } else if (entry.IsPlaying && entry.Source != null && entry.Source.isActiveAndEnabled && !entry.Source.isPlaying) {
                    // the AudioSource was inactive when the audio started
                    entry.Source.Play();
                }
            }
            foreach (var entry in changedEntries) {
                if (entry.IsPlaying) {
                    Stop(entry);
                } else {
                    Play(entry);
                }
            }
        }

        private void Play(Entry entry) {
            if (entry.IsPlaying) {
                return;
            }
            entry.IsPlaying = true;
            SimpleWebRTCLogger.Log($"{name}: receiving audio from {entry.PeerId}.");
            // listeners may activate the AudioSource's parent first, so play afterwards
            AudioStarted?.Invoke(entry.PeerId);
            entry.Source.loop = true;
            if (entry.Source.isActiveAndEnabled) {
                // otherwise Update starts playback once the AudioSource gets activated
                entry.Source.Play();
            }
            AudioSourceStarted?.Invoke(entry.PeerId, entry.Source);
        }

        private void Stop(Entry entry) {
            if (!entry.IsPlaying) {
                return;
            }
            entry.IsPlaying = false;
            if (entry.Source != null) {
                entry.Source.Stop();
            }
            AudioStopped?.Invoke(entry.PeerId);
        }

        private AudioSource CreateAudioSource(string remotePeerId) {
            var parent = spawnParent != null ? spawnParent : transform;
            var source = audioSourcePrefab != null
                ? Instantiate(audioSourcePrefab, parent, false)
                : new GameObject().AddComponent<AudioSource>();
            if (audioSourcePrefab == null) {
                source.transform.SetParent(parent, false);
            }
            source.name = $"{remotePeerId}-Audio";
            source.playOnAwake = false;
            return source;
        }
    }
}
