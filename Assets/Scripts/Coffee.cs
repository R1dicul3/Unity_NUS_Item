using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coffee : MonoBehaviour {
    public EmotionType emotion;
    [SerializeField] private string saveId;

    public string SaveId => string.IsNullOrWhiteSpace(saveId) ? GetHierarchyPath(transform) : saveId;

    void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null || !player.IsWeakerCharacter) return;

        if (EmotionManager.Instance == null) return;

        EmotionManager.Instance.SetEmotion(emotion);
        AudioManager.Instance?.PlayOneShot(SoundType.CollectItem);
        SaveSystem.CollectedStateTracker.MarkCollected(SaveId);
        Destroy(gameObject);
    }

    private void OnValidate() {
        if (string.IsNullOrWhiteSpace(saveId)) {
            saveId = GetHierarchyPath(transform);
        }
    }

    private static string GetHierarchyPath(Transform current) {
        if (current == null) {
            return string.Empty;
        }

        string path = current.name;
        while (current.parent != null) {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
