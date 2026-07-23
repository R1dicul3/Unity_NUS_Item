using UnityEngine;

public class InteractPromptController : MonoBehaviour {
    public static InteractPromptController Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("拖入Canvas下的提示文字UI(包含TextMeshProUGUI的物体)")]
    [SerializeField] private GameObject promptUI;

    [Header("Settings")]
    [Tooltip("提示文字在门上方的偏移量")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);

    private Transform currentTarget;
    private Camera mainCam;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            mainCam = Camera.main;

            if (promptUI != null) {
                promptUI.SetActive(false);
            }
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Update() {
        if (currentTarget != null && promptUI != null && promptUI.activeSelf) {
            promptUI.transform.position = mainCam.WorldToScreenPoint(currentTarget.position + offset);
        }
    }

    public void Show(Transform target) {
        if (promptUI == null) return;

        currentTarget = target;
        promptUI.SetActive(true);
        promptUI.transform.position = mainCam.WorldToScreenPoint(currentTarget.position + offset);
    }

    public void Hide() {
        if (promptUI == null) return;

        currentTarget = null;
        promptUI.SetActive(false);
    }
}