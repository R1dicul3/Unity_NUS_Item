using UnityEngine;

public class EmotionManager : MonoBehaviour {
    public static EmotionManager Instance;

    public EmotionType CurrentEmotion = EmotionType.Calm;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    public void SetEmotion(EmotionType emotion) {
        CurrentEmotion = emotion;

        Debug.Log("Emotion : " + emotion);
    }

    public void SetEmotion(string emotionName) {
        if (System.Enum.TryParse(emotionName, out EmotionType emotion)) {
            SetEmotion(emotion);
        }
        else {
            SetEmotion(EmotionType.Calm);
        }
    }
}
