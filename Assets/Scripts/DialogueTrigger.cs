using UnityEngine;
using UnityEngine.InputSystem;

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

    // ==========================================
    // 【新增】自动翻页配置
    // ==========================================
    [Header("Auto Advance Settings")]
    [Tooltip("开启后，由当前触发器发起的对话会自动翻页。")]
    [SerializeField]
    private bool autoAdvance = false;

    [Tooltip("每句对话停留的时间（秒）。")]
    [SerializeField]
    private float autoAdvanceInterval = 2.0f;

    private float autoAdvanceTimer;
    private bool isExecutingDialogue; // 记录当前对话是否正由本触发器掌控
    // ==========================================

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
        // 1. 处理本触发器发起的对话自动翻页逻辑
        HandleAutoAdvance();

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

        // 如果提示正在显示，且需要自动定位，则实时更新位置
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
        isExecutingDialogue = true; // 标记本触发器正在控制对话
        autoAdvanceTimer = 0f;      // 重置计时器
        HideInteractionPrompt();

        if (lines != null && lines.Length > 0) {
            dialogueController.StartDialogue(lines);
        }
        else {
            dialogueController.StartStoredDialogue();
        }
    }

    // 处理自动翻页核心逻辑
    private void HandleAutoAdvance() {
        // 如果对话 Controller 已经关闭，清理本触发器的对话执行状态
        if (dialogueController == null || !dialogueController.IsShowing) {
            isExecutingDialogue = false;
            autoAdvanceTimer = 0f;
            return;
        }

        // 只有当前对话是由本触发器发起，且勾选了 autoAdvance 时才执行
        if (isExecutingDialogue && autoAdvance) {
            // 如果检测到玩家手动按了按键/鼠标，重置计时器，重新倒计时
            if (IsManualAdvancePressed()) {
                autoAdvanceTimer = 0f;
            }
            else {
                autoAdvanceTimer += Time.deltaTime;
                if (autoAdvanceTimer >= autoAdvanceInterval) {
                    autoAdvanceTimer = 0f;
                    dialogueController.Advance(); // 触发下一句
                }
            }
        }
    }

    // 检查玩家是否进行了手动翻页操作
    private bool IsManualAdvancePressed() {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (inputActions != null && inputActions.Player.NextDialogue.WasPressedThisFrame())
            || (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
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

    private void UpdatePromptPosition() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 targetWorldPos = transform.position + promptOffset;

        Canvas parentCanvas = interactionPrompt.GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace) {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);
            interactionPrompt.transform.position = screenPos;
        }
        else {
            interactionPrompt.transform.position = targetWorldPos;
        }
    }
}