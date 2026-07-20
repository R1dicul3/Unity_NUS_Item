using UnityEngine;

public class EmotionManager : MonoBehaviour {
    public static EmotionManager Instance;

    public EmotionType CurrentEmotion = EmotionType.Calm;

    private void Awake() {
        if (Instance == null)
            Instance = this;
    }

    public void SetEmotion(EmotionType emotion) {
        CurrentEmotion = emotion;

        Debug.Log("Emotion : " + emotion);
    }
}