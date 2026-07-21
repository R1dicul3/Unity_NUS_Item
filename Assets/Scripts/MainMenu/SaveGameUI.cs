using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    public class SaveGameUI : MonoBehaviour
    {
        [Header("Appearance")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Color titleColor = Color.white;
        public Font overrideFont;

        [Header("Layout")]
        [Range(24, 100)] public int titleFontSize = 56;
        [Range(16, 64)] public int slotFontSize = 22;
        [Range(16, 64)] public int actionButtonFontSize = 24;
        [Range(200, 800)] public float slotWidth = 600f;
        [Range(30, 120)] public float slotHeight = 75f;
        [Range(0, 60)] public float spacing = 12f;

        [Header("Save Slots")]
        [Range(1, 10)] public int saveSlotCount = 3;

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private PlayerInputActions inputActions;
        private int selectedSlot = -1;
        private Button[] slotButtons;
        private Image[] slotButtonImages;
        private Button saveButton;
        private GameObject messageObject;
        private Transform messageRoot;
        private Graphic messageGraphic;

        private void Awake()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Menu.performed += OnMenuPerformed;
            inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                return;
            }

            inputActions.Player.Menu.performed -= OnMenuPerformed;
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            if (FindFirstObjectByType<ConfirmDialogUI>() != null)
            {
                return;
            }

            OnCancelClicked();
        }

        private void BuildUI()
        {
            ClampSaveSlotCount();

            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 200;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, slotWidth, spacing);

            MenuUIHelper.CreateText(content, "Save Game", titleFontSize, titleColor, EffectiveFont, FontStyle.Bold, 100f);

            slotButtons = new Button[saveSlotCount];
            slotButtonImages = new Image[saveSlotCount];

            for (int i = 0; i < saveSlotCount; i++)
            {
                int slot = i + 1;
                Button button = MenuUIHelper.CreateButton(content, GetSlotLabel(slot), slotFontSize, slotHeight,
                    buttonColor, () => OnSlotClicked(slot), EffectiveFont, true);

                slotButtons[i] = button;
                slotButtonImages[i] = button.GetComponent<Image>();
            }

            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content, false);
            spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(slotWidth, 20f);

            GameObject actionContainer = new GameObject("ActionContainer");
            actionContainer.transform.SetParent(content, false);
            actionContainer.AddComponent<RectTransform>().sizeDelta = new Vector2(slotWidth, 55f);

            saveButton = MenuUIHelper.CreateButton(actionContainer.transform, "Save", actionButtonFontSize, 55f,
                new Color(0.2f, 0.55f, 0.3f, 1f), OnSaveClicked, EffectiveFont, true);
            RectTransform saveRect = saveButton.GetComponent<RectTransform>();
            saveRect.anchorMin = new Vector2(0f, 0.5f);
            saveRect.anchorMax = new Vector2(0f, 0.5f);
            saveRect.pivot = new Vector2(0f, 0.5f);
            saveRect.anchoredPosition = Vector2.zero;
            saveRect.sizeDelta = new Vector2(260f, 55f);

            Button cancelButton = MenuUIHelper.CreateButton(actionContainer.transform, "Cancel", actionButtonFontSize, 55f,
                new Color(0.55f, 0.2f, 0.2f, 1f), OnCancelClicked, EffectiveFont, true);
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(1f, 0.5f);
            cancelRect.anchorMax = new Vector2(1f, 0.5f);
            cancelRect.pivot = new Vector2(1f, 0.5f);
            cancelRect.anchoredPosition = Vector2.zero;
            cancelRect.sizeDelta = new Vector2(260f, 55f);

            CreateMessageArea(canvas.transform);
            RefreshSlotHighlight();
        }

        private bool TryBuildPrefabUI()
        {
            ClampSaveSlotCount();

            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/SaveGamePanelCanvas");

            if (prefab == null)
            {
                return false;
            }

            GameObject canvas = Instantiate(prefab, transform);
            canvas.name = prefab.name;
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();

            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            if (canvasComponent != null)
            {
                canvasComponent.sortingOrder = 200;
            }

            slotButtons = new Button[saveSlotCount];
            slotButtonImages = new Image[saveSlotCount];

            bool hasRequiredControls = true;
            for (int i = 0; i < saveSlotCount; i++)
            {
                int slot = i + 1;
                hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, $"SlotButton_{slot}",
                    () => OnSlotClicked(slot), out slotButtons[i]);
                slotButtonImages[i] = slotButtons[i] != null ? slotButtons[i].GetComponent<Image>() : null;
            }

            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "SaveButton", OnSaveClicked, out saveButton);
            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "CancelButton", OnCancelClicked, out _);
            BindMessage(canvas.transform);

            if (!hasRequiredControls)
            {
                Debug.LogWarning("[SaveGameUI] Save prefab is missing expected controls. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            RefreshUI();
            return true;
        }

        private string GetSlotLabel(int slot)
        {
            SaveSystem.SaveMetaInfo meta = SaveSystem.SaveSystem.GetMetaInfo(slot);
            return meta == null
                ? $"Slot {slot}  (Empty)"
                : $"Slot {slot}  |  {meta.saveTimestamp}  |  Play Time: {meta.GetFormattedPlayTime()}";
        }

        private void OnSlotClicked(int slot)
        {
            selectedSlot = slot;
            RefreshSlotHighlight();
        }

        private void RefreshSlotHighlight()
        {
            if (slotButtonImages == null)
            {
                return;
            }

            for (int i = 0; i < slotButtonImages.Length; i++)
            {
                if (slotButtonImages[i] == null)
                {
                    continue;
                }

                bool isSelected = (i + 1) == selectedSlot;
                slotButtonImages[i].color = isSelected
                    ? new Color(buttonColor.r * 1.3f, buttonColor.g * 1.3f, buttonColor.b * 1.3f, 1f)
                    : buttonColor;
            }

            if (saveButton != null)
            {
                saveButton.interactable = selectedSlot >= 1;
            }
        }

        private void OnSaveClicked()
        {
            if (selectedSlot < 1)
            {
                ShowMessage("Please select a save slot first.", Color.yellow);
                return;
            }

            if (SaveSystem.SaveSystem.HasSave(selectedSlot))
            {
                ConfirmDialogUI.Show($"Save slot {selectedSlot} already has data. Overwrite it?",
                    onConfirm: () => DoSave(selectedSlot),
                    onCancel: null);
                return;
            }

            DoSave(selectedSlot);
        }

        private void DoSave(int slot)
        {
            SaveSystem.SaveData data = GamePauseManager.Instance != null
                ? GamePauseManager.Instance.CreateSaveData()
                : new SaveSystem.SaveData
                {
                    saveVersion = 2,
                    playerPosition = new SaveSystem.SerializableVector3(Vector3.zero),
                    playTimeSeconds = SaveSystem.GameTimer.Instance?.GetElapsedTime() ?? 0f,
                    saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    sceneName = SceneManager.GetActiveScene().name
                };

            SaveSystem.SaveSystem.Save(slot, data);
            GamePauseManager.Instance?.MarkProgressSaved();
            ShowMessage("Saved.", Color.green);
            RefreshUI();
            Invoke(nameof(Close), 1.5f);
        }

        private void OnCancelClicked()
        {
            CancelInvoke(nameof(Close));
            GamePauseManager.Instance?.OnSaveGameUIClosed();
            Destroy(gameObject);
        }

        private void CreateMessageArea(Transform canvasTransform)
        {
            GameObject message = new GameObject("MessageText");
            message.transform.SetParent(canvasTransform, false);

            Text text = message.AddComponent<Text>();
            text.text = "";
            text.font = EffectiveFont;
            text.fontSize = 26;
            text.color = Color.green;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = message.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.15f);
            rect.anchorMax = new Vector2(0.5f, 0.15f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 50f);

            messageRoot = message.transform;
            messageObject = message;
            messageGraphic = text;
            SetMessageVisible(false);
        }

        private void BindMessage(Transform root)
        {
            Transform message = MenuUIHelper.FindChildRecursive(root, "MessageText");
            if (message == null)
            {
                return;
            }

            messageRoot = message;
            messageObject = message.gameObject;
            messageGraphic = message.GetComponent<Graphic>() ?? message.GetComponentInChildren<Graphic>(true);
            SetMessageVisible(false);
        }

        private void ShowMessage(string message, Color color)
        {
            if (messageRoot == null)
            {
                return;
            }

            MenuUIHelper.TrySetText(messageRoot, message);
            if (messageGraphic != null)
            {
                messageGraphic.color = color;
            }

            SetMessageVisible(true);
        }

        private void SetMessageVisible(bool value)
        {
            if (messageObject != null)
            {
                messageObject.SetActive(value);
            }
        }

        private void RefreshUI()
        {
            if (slotButtons == null)
            {
                return;
            }

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                {
                    MenuUIHelper.TrySetText(slotButtons[i].transform, GetSlotLabel(i + 1));
                }
            }

            RefreshSlotHighlight();
        }

        private void Close()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            GamePauseManager.Instance?.OnSaveGameUIClosed();
            Destroy(gameObject);
        }

        private void ClampSaveSlotCount()
        {
            saveSlotCount = Mathf.Clamp(saveSlotCount, 1, SaveSystem.SaveSystem.GetMaxSlots());
        }
    }
}
