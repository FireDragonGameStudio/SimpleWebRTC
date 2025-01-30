using UnityEngine;

public class ContinuousRotation : MonoBehaviour {
    [Header("Rotation Speed (Degrees per Second)")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 36, 0);

    [SerializeField] private bool randomColor = false;

    private void Start() {
        if (randomColor) {
            GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
        }
    }

    private void Update() {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}