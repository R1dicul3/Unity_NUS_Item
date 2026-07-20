using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coffee : MonoBehaviour {
    public EmotionType emotion;

    void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null || !player.IsWeakerCharacter) return;

        if (EmotionManager.Instance == null) return;

        EmotionManager.Instance.SetEmotion(emotion);
        Destroy(gameObject);
    }
}