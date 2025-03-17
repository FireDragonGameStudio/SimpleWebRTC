using SimpleWebRTC;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleWebRTC {
    public class UniquePeerId : MonoBehaviour {

        [SerializeField] private string peerId;
        [SerializeField] private WebRTCConnection webRTCConnection;

        private static HashSet<string> usedNames = new HashSet<string>();

        private void Awake() {
            peerId = GenerateUniquePeerId();
            Debug.Log("Generated Peer ID: " + peerId);
            webRTCConnection.SetUniquePlayerName(peerId);
        }

        private string GenerateUniquePeerId() {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            System.Random rand = new System.Random();

            string name;
            do {
                int length = rand.Next(3, 6); // Generates a length between 3 and 5
                char[] nameChars = new char[length];

                for (int i = 0; i < length; i++) {
                    nameChars[i] = chars[rand.Next(chars.Length)];
                }

                name = new string(nameChars) + "-PeerId";
            }
            while (usedNames.Contains(name));

            usedNames.Add(name);
            return name;
        }
    }
}