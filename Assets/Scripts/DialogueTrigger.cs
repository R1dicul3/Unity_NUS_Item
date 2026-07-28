using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour {
    [Header("Dialogue Reference")]
    [SerializeField]
    private DialogueController dialogueController;

    [Header("Dialogue Content")]
    [Tooltip("��������ר���ĶԻ����ݣ�֧��˫������������ã���������գ������� DialogueController Ĭ�ϵĶԻ���")]
    [SerializeField]
    private DialogueController.DialogueLine[] lines;

    [Header("Localization")]
    [SerializeField]
    private string dialogueGroupId;

    [Header("Trigger Behaviour")]
    [Tooltip("��ҽ�����ײ������Ƿ���Ҫ�� NextDialogue �ſ�ʼ�Ի���")]
    [SerializeField]
    private bool requireInteraction = true;

    [Tooltip("�Ƿ�ֻ�ܴ���һ�Ρ�")]
    [SerializeField]
    private bool triggerOnce = false;

    // ==========================================
    // ���������Զ���ҳ����
    // ==========================================
    [Header("Auto Advance Settings")]
    [Tooltip("�������ɵ�ǰ����������ĶԻ����Զ���ҳ��")]
    [SerializeField]
    private bool autoAdvance = false;

    [Tooltip("ÿ��Ի�ͣ����ʱ�䣨�룩��")]
    [SerializeField]
    private float autoAdvanceInterval = 2.0f;

    private float autoAdvanceTimer;
    private bool isExecutingDialogue; // ��¼��ǰ�Ի��Ƿ����ɱ��������ƿ�
    // ==========================================

    [Header("Optional Interaction Prompt")]
    [Tooltip("�Ƿ���ʾ������ʾ��")]
    [SerializeField]
    private bool showInteractionPrompt = true;

    [Tooltip("������ʾ UI�������� Canvas ��� UI��Ҳ�����ǳ������ Sprite/3D Text����")]
    [SerializeField]
    private GameObject interactionPrompt;

    [Tooltip("������ÿ����ʾ����ʱ���Զ��ƶ�����ǰ���壨NPC/���䣩���Ϸ���")]
    [SerializeField]
    private bool autoPositionPrompt = true;

    [Tooltip("��ʾͼ������ڵ�ǰ�����ƫ���������� Y=1.5 ��ʾ�������Ϸ� 1.5 ��λ����")]
    [SerializeField]
    private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

    private bool playerInside;
    private bool dialogueTriggered;
    private bool promptConsumed;

    private PlayerInputActions inputActions;
    private Camera mainCamera; // �������������������

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
        // 1. ����������������ĶԻ��Զ���ҳ�߼�
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

        // �����ʾ������ʾ������Ҫ�Զ���λ����ʵʱ����λ��
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


        dialogueTriggered = true;
        promptConsumed = true;
        isExecutingDialogue = true; // ��Ǳ����������ڿ��ƶԻ�
        autoAdvanceTimer = 0f;      // ���ü�ʱ��
        HideInteractionPrompt();

        if (lines != null && lines.Length > 0) {
            dialogueController.StartDialogue(lines, dialogueGroupId);
        }
        else {
            dialogueController.StartStoredDialogue();
        }
    }

    // �����Զ���ҳ�����߼�
    private void HandleAutoAdvance() {
        // ����Ի� Controller �Ѿ��رգ��������������ĶԻ�ִ��״̬
        if (dialogueController == null || !dialogueController.IsShowing) {
            isExecutingDialogue = false;
            autoAdvanceTimer = 0f;
            return;
        }

        // ֻ�е�ǰ�Ի����ɱ������������ҹ�ѡ�� autoAdvance ʱ��ִ��
        if (isExecutingDialogue && autoAdvance) {
            // �����⵽����ֶ����˰���/��꣬���ü�ʱ�������µ���ʱ
            if (IsManualAdvancePressed()) {
                autoAdvanceTimer = 0f;
            }
            else {
                autoAdvanceTimer += Time.deltaTime;
                if (autoAdvanceTimer >= autoAdvanceInterval) {
                    autoAdvanceTimer = 0f;
                    dialogueController.Advance(); // ������һ��
                }
            }
        }
    }

    // �������Ƿ�������ֶ���ҳ����
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