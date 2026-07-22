using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 存档（Load Game）界面 UI 构建器。
    /// 显示最多 3 个存档槽，已存在的存档高亮可用，空槽置灰。
    /// 顶部有标题，底部有返回按钮。
    /// </summary>
    public class LoadGameUI : MonoBehaviour
    {
        [Header("外观设置")]
        public Color backgroundColor = Color.white;
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Color titleColor = Color.black;
        public Font overrideFont;

        [Header("尺寸设置")]
        [Range(24, 100)]
        public int titleFontSize = 56;

        [Range(16, 64)]
        public int slotFontSize = 22;

        [Range(16, 64)]
        public int actionButtonFontSize = 24;

        [Range(16, 64)]
        public int backButtonFontSize = 24;

        [Range(200, 800)]
        public float slotWidth = 600f;

        [Range(30, 120)]
        public float slotHeight = 75f;

        [Range(0, 60)]
        public float spacing = 12f;

        [Header("存档设置")]
        [Range(1, 10)]
        public int saveSlotCount = 3;

        public string saveFileExtension = ".json";

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private PlayerInputActions inputActions;
        private int selectedSlot = -1;
        private Button[] slotButtons;
        private Image[] slotButtonImages;
        private Button loadButton;
        private Button deleteButton;

        void Awake()
        {
            BuildUI();
        }

        void OnEnable()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Menu.performed += OnMenuPerformed;
            inputActions.Enable();
        }

        void OnDisable()
        {
            if (inputActions != null)
            {
                inputActions.Player.Menu.performed -= OnMenuPerformed;
                inputActions.Disable();
                inputActions.Dispose();
                inputActions = null;
            }
        }

        void OnMenuPerformed(InputAction.CallbackContext context)
        {
            if (FindFirstObjectByType<ConfirmDialogUI>() != null)
            {
                return;
            }

            OnBackClicked();
        }

        void BuildUI()
        {
            ClampSaveSlotCount();

            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, slotWidth, spacing);

            // 标题
            MenuUIHelper.CreateText(content, "Select Save", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 100f);

            // 存档槽
            slotButtons = new Button[saveSlotCount];
            slotButtonImages = new Image[saveSlotCount];

            for (int i = 0; i < saveSlotCount; i++)
            {
                int slot = i + 1;
                string label = GetSlotLabel(slot);
                bool hasSave = SaveSystem.SaveSystem.HasSave(slot);

                Button btn = MenuUIHelper.CreateButton(content, label, slotFontSize, slotHeight,
                    buttonColor, () => OnSlotClicked(slot), EffectiveFont, true);

                slotButtons[i] = btn;
                slotButtonImages[i] = btn.GetComponent<Image>();

                // 空槽稍微变暗以示区别
                if (!hasSave && slotButtonImages[i] != null)
                {
                    slotButtonImages[i].color = new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f, 0.5f);
                }
            }

            // 间距
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(slotWidth, 20f);

            // 操作按钮容器
            GameObject actionContainer = new GameObject("ActionContainer");
            actionContainer.transform.SetParent(content, false);
            RectTransform actionRect = actionContainer.AddComponent<RectTransform>();
            actionRect.sizeDelta = new Vector2(slotWidth, 55f);

            // Load 按钮
            Button loadBtn = MenuUIHelper.CreateButton(actionContainer.transform, "Load", actionButtonFontSize, 55f,
                new Color(0.2f, 0.55f, 0.3f, 1f), OnLoadClicked, EffectiveFont, true);
            loadButton = loadBtn;
            RectTransform loadRect = loadBtn.GetComponent<RectTransform>();
            loadRect.anchorMin = new Vector2(0f, 0.5f);
            loadRect.anchorMax = new Vector2(0f, 0.5f);
            loadRect.pivot = new Vector2(0f, 0.5f);
            loadRect.anchoredPosition = Vector2.zero;
            loadRect.sizeDelta = new Vector2(170f, 55f);

            // Delete 按钮
            Button deleteBtn = MenuUIHelper.CreateButton(actionContainer.transform, "Delete", actionButtonFontSize, 55f,
                new Color(0.55f, 0.2f, 0.2f, 1f), OnDeleteClicked, EffectiveFont, true);
            deleteButton = deleteBtn;
            RectTransform deleteRect = deleteBtn.GetComponent<RectTransform>();
            deleteRect.anchorMin = new Vector2(0.5f, 0.5f);
            deleteRect.anchorMax = new Vector2(0.5f, 0.5f);
            deleteRect.pivot = new Vector2(0.5f, 0.5f);
            deleteRect.anchoredPosition = Vector2.zero;
            deleteRect.sizeDelta = new Vector2(170f, 55f);

            // 返回按钮
            Button backBtn = MenuUIHelper.CreateButton(actionContainer.transform, "< Back", backButtonFontSize, 55f,
                buttonColor, OnBackClicked, EffectiveFont, true);
            RectTransform backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(1f, 0.5f);
            backRect.anchoredPosition = Vector2.zero;
            backRect.sizeDelta = new Vector2(170f, 55f);

            RefreshSlotHighlight();
        }

        bool TryBuildPrefabUI()
        {
            ClampSaveSlotCount();

            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/LoadGamePanelCanvas");

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
                hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, $"SlotButton_{slot}", () => OnSlotClicked(slot), out slotButtons[i]);
                slotButtonImages[i] = slotButtons[i] != null ? slotButtons[i].GetComponent<Image>() : null;
            }

            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "LoadButton", OnLoadClicked, out loadButton);
            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "DeleteButton", OnDeleteClicked, out deleteButton);
            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "BackButton", OnBackClicked, out _);

            if (!hasRequiredControls)
            {
                Debug.LogWarning("[LoadGameUI] Load game prefab is missing one or more expected controls. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            RefreshUI();
            return true;
        }

        string GetSlotLabel(int slot)
        {
            var meta = SaveSystem.SaveSystem.GetMetaInfo(slot);
            if (meta == null)
            {
                return $"Slot {slot}  (Empty)";
            }

            return $"Slot {slot}  |  {meta.saveTimestamp}  |  Play Time: {meta.GetFormattedPlayTime()}";
        }

        void OnSlotClicked(int slot)
        {
            selectedSlot = slot;
            RefreshSlotHighlight();
        }

        void RefreshSlotHighlight()
        {
            for (int i = 0; i < slotButtonImages.Length; i++)
            {
                if (slotButtonImages[i] == null) continue;

                int slot = i + 1;
                bool hasSave = SaveSystem.SaveSystem.HasSave(slot);
                bool isSelected = slot == selectedSlot;

                if (isSelected)
                {
                    slotButtonImages[i].color = new Color(buttonColor.r * 1.3f, buttonColor.g * 1.3f, buttonColor.b * 1.3f, 1f);
                }
                else if (!hasSave)
                {
                    slotButtonImages[i].color = new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f, 0.5f);
                }
                else
                {
                    slotButtonImages[i].color = buttonColor;
                }
            }

            bool selectedHasSave = selectedSlot >= 1 && SaveSystem.SaveSystem.HasSave(selectedSlot);
            if (loadButton != null)
            {
                loadButton.interactable = selectedHasSave;
            }

            if (deleteButton != null)
            {
                deleteButton.interactable = selectedHasSave;
            }
        }

        void OnLoadClicked()
        {
            if (selectedSlot < 1)
            {
                Debug.Log("[LoadGameUI] Please select a save slot first.");
                return;
            }

            if (!SaveSystem.SaveSystem.HasSave(selectedSlot))
            {
                Debug.Log("[LoadGameUI] Selected slot is empty, cannot load.");
                return;
            }

            GamePauseManager.Instance?.LoadSaveGame(selectedSlot);
        }

        void OnDeleteClicked()
        {
            if (selectedSlot < 1)
            {
                Debug.Log("[LoadGameUI] Please select a save slot first.");
                return;
            }

            if (!SaveSystem.SaveSystem.HasSave(selectedSlot))
            {
                Debug.Log("[LoadGameUI] Selected slot is empty, cannot delete.");
                return;
            }

            ConfirmDialogUI.Show($"Are you sure you want to delete save slot {selectedSlot}? This action cannot be undone.",
                onConfirm: () =>
                {
                    SaveSystem.SaveSystem.Delete(selectedSlot);
                    selectedSlot = -1;
                    RefreshUI();
                },
                onCancel: null,
                dialogSound: SoundType.UIAlert);
        }

        void OnBackClicked()
        {
            if (GamePauseManager.Instance != null && GamePauseManager.Instance.CameFromPauseMenu)
            {
                GamePauseManager.Instance.ReturnToGameFromLoadGame();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        void RefreshUI()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i + 1;
                MenuUIHelper.TrySetText(slotButtons[i].transform, GetSlotLabel(slot));
            }
            RefreshSlotHighlight();
        }

        void ClampSaveSlotCount()
        {
            saveSlotCount = Mathf.Clamp(saveSlotCount, 1, SaveSystem.SaveSystem.GetMaxSlots());
        }
    }
}
