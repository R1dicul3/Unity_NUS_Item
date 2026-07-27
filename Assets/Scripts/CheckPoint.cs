using UnityEngine;

public class FinalRoomCheck : MonoBehaviour {
    [Header("需要出现的 GameObject")]
    [SerializeField] private GameObject objectToAppear;

    private bool hasTriggered = false;

    private void Start() {
        // 确保目标物体一开始隐藏
        if (objectToAppear != null) {
            objectToAppear.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (hasTriggered) {
            return;
        }

        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player == null) {
            return;
        }

        hasTriggered = true;

        if (objectToAppear != null) {
            objectToAppear.SetActive(true);
        }
    }
}