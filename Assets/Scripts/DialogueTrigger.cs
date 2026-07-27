using UnityEngine;

public class DialogueTrigger : MonoBehaviour {
    [Header("Dialogue Reference")]
    [SerializeField]
    private DialogueController dialogueController;

    [Header("Dialogue Content")]
    [Tooltip("本触发器专属的对话内容（支持双立绘与高亮配置）。如果留空，将播放 DialogueController 默认的对话。")]
    [SerializeField]
    private DialogueController.DialogueLine[] lines;

    [Header("Trigger Behaviour")]
    [Tooltip("玩家进入碰撞区域后是否需要按 NextDialogue 才开始对话。")]
    [SerializeField]
    private bool requireInteraction = true;

    [Tooltip("是否只能触发一次。")]
    [SerializeField]
    private bool triggerOnce = false;

    [Header("Optional Interaction Prompt")]
    [Tooltip("是否显示交互提示。")]
    [SerializeField]
    private bool showInteractionPrompt = true;

    [Tooltip("交互提示 UI（可以是 Canvas 里的 UI，也可以是场景里的 Sprite/3D Text）。")]
    [SerializeField]
    private GameObject interactionPrompt;

    [Tooltip("开启后，每次提示出现时会自动移动到当前物体（NPC/宝箱）的上方。")]
    [SerializeField]
    private bool autoPositionPrompt = true;

    [Tooltip("提示图标相对于当前物体的偏移量。例如 Y=1.5 表示在物体上方 1.5 单位处。")]
    [SerializeField]
    private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

    private bool playerInside;
    private bool dialogueTriggered;
    private bool promptConsumed;

    private PlayerInputActions inputActions;
    private Camera mainCamera; // 缓存主相机以提升性能

    private void Awake() {
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
        HideInteractionPrompt();
    }

    private void Start() {
        if (dialogueController == null) {
            dialogueController = FindFirstObjectByType<DialogueController>();
        }
    }

    private void OnEnable() {
        inputActions?.Enable();
    }

    private void OnDisable() {
        inputActions?.Disable();
    }

    private void OnDestroy() {
        inputActions?.Dispose();
    }

    private void Update() {
        if (!playerInside) {
            return;
        }

        if (triggerOnce && dialogueTriggered) {
            return;
        }

        if (dialogueController != null && dialogueController.IsShowing) {
            if (!promptConsumed) {
                promptConsumed = true;
                HideInteractionPrompt();
            }
            return;
        }

        // 【新增逻辑】：如果提示正在显示，且需要自动定位，则实时更新位置（防止相机移动时 UI 错位）
        if (showInteractionPrompt && !promptConsumed && interactionPrompt != null && interactionPrompt.activeSelf && autoPositionPrompt) {
            UpdatePromptPosition();
        }

        if (!requireInteraction) {
            return;
        }

        if (!promptConsumed && inputActions.Player.NextDialogue.WasPressedThisFrame()) {
            StartDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) {
            return;
        }

        if (triggerOnce && dialogueTriggered) {
            return;
        }

        playerInside = true;
        promptConsumed = false;

        if (showInteractionPrompt) {
            ShowInteractionPrompt();
        }

        if (!requireInteraction) {
            StartDialogue();
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Player")) {
            return;
        }

        playerInside = false;
        HideInteractionPrompt();
    }

    private void StartDialogue() {
        if (triggerOnce && dialogueTriggered) {
            return;
        }

        if (dialogueController == null) {
            Debug.LogWarning($"{name}: DialogueController is not assigned or found in scene.");
            return;
        }

        if (dialogueController.IsShowing) {
            return;
        }

        dialogueTriggered = true;
        promptConsumed = true;
        HideInteractionPrompt();

        if (lines != null && lines.Length > 0) {
            dialogueController.StartDialogue(lines);
        }
        else {
            dialogueController.StartStoredDialogue();
        }
    }

    private void ShowInteractionPrompt() {
        if (interactionPrompt != null && !promptConsumed) {
            if (autoPositionPrompt) {
                UpdatePromptPosition();
            }
            interactionPrompt.SetActive(true);
        }
    }

    private void HideInteractionPrompt() {
        if (interactionPrompt != null) {
            interactionPrompt.SetActive(false);
        }
    }

    // 【新增方法】：处理世界坐标到屏幕坐标的正确转换
    private void UpdatePromptPosition() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 targetWorldPos = transform.position + promptOffset;

        // 检查这个交互提示是不是 UI Canvas 里的元素
        Canvas parentCanvas = interactionPrompt.GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace) {
            // 如果是屏幕空间的 UI (Screen Space - Overlay / Camera)
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);
            interactionPrompt.transform.position = screenPos;
        }
        else {
            // 如果是世界空间的物体 (比如场景里直接放的 Sprite、3D TextMeshPro)
            interactionPrompt.transform.position = targetWorldPos;
        }
    }
}