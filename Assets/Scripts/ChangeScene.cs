using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour {
    [SerializeField] private string targetScene = "Scene_2";

    private void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player == null) {
            return;
        }

        SceneManager.LoadScene(targetScene);
    }
}