using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class TutorialMessageTrigger : MonoBehaviour {
    [Header("UI 引用")]
    [Tooltip("指向专属的教程文本组件（不要和门提示UI共用同一个UI对象）")]
    [SerializeField] private TextMeshProUGUI tutorialTextUI;

    [Header("教程设置")]
    [TextArea(3, 5)]
    [Tooltip("在这个区域要显示的教程文字")]
    [SerializeField] private string messageToShow;

    [Tooltip("文字显示的最短时间（秒）")]
    [SerializeField] private float minDisplayDuration = 2f;

    [Tooltip("是否需要按下交互键才显示文字 (设为 false 则碰到立刻显示)")]
    [SerializeField] private bool requireInteraction = true;

    [Header("位置设置")]
    [Tooltip("是否将文字动态显示在这个物体上方？")]
    [SerializeField] private bool showAboveObject = true;

    [Tooltip("文字在物体上方的偏移量 (X, Y, Z)")]
    [SerializeField] private Vector3 textOffset = new Vector3(0f, 2.5f, 0f);

    private PlayerInputActions inputActions;
    private bool isPlayerInZone;
    private bool isMessageVisible;
    private float showStartTime;
    private Coroutine hideCoroutine;
    private Camera mainCamera;

    private void Awake() {
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() {
        inputActions.Disable();
        ResetState();
    }
    private void OnDestroy() => inputActions.Dispose();

    private void Start() {
        if (tutorialTextUI != null && !isMessageVisible) {
            tutorialTextUI.gameObject.SetActive(false);
        }
    }

    private void Update() {
        if (!isPlayerInZone || !requireInteraction || isMessageVisible) return;

        if (inputActions.Player.Interact.WasPressedThisFrame()) {
            ShowTutorialMessage();
        }
    }

    private void LateUpdate() {
        if (isMessageVisible && showAboveObject && tutorialTextUI != null && mainCamera != null) {
            Vector3 targetWorldPosition = transform.position + textOffset;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetWorldPosition);
            tutorialTextUI.transform.position = screenPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInZone = true;

            // 注意：这里我们移除了全局 InteractPromptController 的调用，
            // 这样就不会污染 RoomDoor 的按键提示了。
            if (!requireInteraction) {
                ShowTutorialMessage();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInZone = false;

            if (isMessageVisible) {
                float timeAlreadyShown = Time.time - showStartTime;
                float timeRemaining = minDisplayDuration - timeAlreadyShown;

                if (timeRemaining > 0) {
                    hideCoroutine = StartCoroutine(HideTextAfterDelay(timeRemaining));
                }
                else {
                    HideText();
                }
            }
        }
    }

    private void ShowTutorialMessage() {
        if (tutorialTextUI == null) return;

        isMessageVisible = true;
        showStartTime = Time.time;

        tutorialTextUI.text = messageToShow;
        tutorialTextUI.gameObject.SetActive(true);

        if (showAboveObject && mainCamera != null) {
            tutorialTextUI.transform.position = mainCamera.WorldToScreenPoint(transform.position + textOffset);
        }

        if (hideCoroutine != null) {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    private IEnumerator HideTextAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (!isPlayerInZone) HideText();
    }

    private void HideText() {
        if (tutorialTextUI != null && tutorialTextUI.text == messageToShow) {
            tutorialTextUI.gameObject.SetActive(false);
        }
        isMessageVisible = false;
    }

    private void ResetState() {
        isPlayerInZone = false;
        if (hideCoroutine != null) {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        HideText();
    }
}