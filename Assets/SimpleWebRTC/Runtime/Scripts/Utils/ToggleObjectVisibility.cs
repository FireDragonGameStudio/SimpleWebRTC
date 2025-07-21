using UnityEngine;

public class ToggleObjectVisibility : MonoBehaviour {
    [SerializeField] private GameObject objectToToggle;

    public void ToggleVisibility() {
        if (objectToToggle != null) {
            objectToToggle.SetActive(!objectToToggle.activeSelf);
        }
    }
}
