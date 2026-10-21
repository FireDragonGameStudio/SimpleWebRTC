using UnityEngine;

namespace SimpleWebRTC {
    public class ToggleObjectVisibility : MonoBehaviour {
        [SerializeField] private GameObject objectToToggle;

        public void ToggleVisibility() {
            if (objectToToggle != null) {
                objectToToggle.SetActive(!objectToToggle.activeSelf);
            }
        }
    }
}