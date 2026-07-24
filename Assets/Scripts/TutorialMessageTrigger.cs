using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class TutorialMessageTrigger : MonoBehaviour {
    [Header("UI Reference")]
    [Tooltip("Dedicated tutorial text component. Do not share the same UI object with the door prompt UI.")]
    [SerializeField] private TextMeshProUGUI tutorialTextUI;

    [Header("Tutorial Settings")]
    [TextArea(3, 5)]
    [Tooltip("Tutorial text shown when the player uses this trigger.")]
    [SerializeField] private string messageToShow;

    [Tooltip("Minimum time that the text stays visible, in seconds.")]
    [SerializeField] private float minDisplayDuration = 2f;

    [Tooltip("If true, the player must press Interact while inside the trigger. If false, entering the trigger shows the text immediately.")]
    [SerializeField] private bool requireInteraction = true;

    [Header("Position Settings")]
    [Tooltip("If true, dynamically positions the tutorial text above this object.")]
    [SerializeField] private bool showAboveObject = true;

    [Tooltip("World-space offset used when placing the text above this object.")]
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

            // Keep this separate from the global InteractPromptController so tutorial text does not overwrite door prompts.
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
