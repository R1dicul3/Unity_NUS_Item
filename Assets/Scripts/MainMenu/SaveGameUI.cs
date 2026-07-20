using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 保存游戏界面 UI。
    /// 由 GamePauseManager 在当前场景中动态创建，不加载新场景。
    /// </summary>
    public class SaveGameUI : MonoBehaviour
    {
        [Header("外观设置")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Color titleColor = Color.white;
        public Font overrideFont;

        [Header("尺寸设置")]
        [Range(24, 100)]
        public int titleFontSize = 56;

        [Range(16, 64)]
        public int slotFontSize = 22;

        [Range(16, 64)]
        public int actionButtonFontSize = 24;

        [Range(200, 800)]
        public float slotWidth = 600f;

        [Range(30, 120)]
        public float slotHeight = 75f;

        [Range(0, 60)]
        public float spacing = 12f;

        [Header("存档设置")]
        [Range(1, 10)]
        public int saveSlotCount = 3;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private PlayerInputActions inputActions;
        private int selectedSlot = -1;
        private Button[] slotButtons;
        private Image[] slotButtonImages;
        private Text messageText;
        private GameObject messageObject;

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
            OnCancelClicked();
        }

        void BuildUI()
        {
            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 200;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, slotWidth, spacing);

            // 标题
            MenuUIHelper.CreateText(content, "Save Game", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 100f);

            // 存档槽
            slotButtons = new Button[saveSlotCount];
            slotButtonImages = new Image[saveSlotCount];

            for (int i = 0; i < saveSlotCount; i++)
            {
                int slot = i + 1;
                string label = GetSlotLabel(slot);

                Button btn = MenuUIHelper.CreateButton(content, label, slotFontSize, slotHeight,
                    buttonColor, () => OnSlotClicked(slot), EffectiveFont, true);

                slotButtons[i] = btn;
                slotButtonImages[i] = btn.GetComponent<Image>();
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

            // Save 按钮
            Button saveBtn = MenuUIHelper.CreateButton(actionContainer.transform, "Save", actionButtonFontSize, 55f,
                new Color(0.2f, 0.55f, 0.3f, 1f), OnSaveClicked, EffectiveFont, true);
            RectTransform saveRect = saveBtn.GetComponent<RectTransform>();
            saveRect.anchorMin = new Vector2(0f, 0.5f);
            saveRect.anchorMax = new Vector2(0f, 0.5f);
            saveRect.pivot = new Vector2(0f, 0.5f);
            saveRect.anchoredPosition = Vector2.zero;
            saveRect.sizeDelta = new Vector2(260f, 55f);

            // Cancel 按钮
            Button cancelBtn = MenuUIHelper.CreateButton(actionContainer.transform, "Cancel", actionButtonFontSize, 55f,
                new Color(0.55f, 0.2f, 0.2f, 1f), OnCancelClicked, EffectiveFont, true);
            RectTransform cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(1f, 0.5f);
            cancelRect.anchorMax = new Vector2(1f, 0.5f);
            cancelRect.pivot = new Vector2(1f, 0.5f);
            cancelRect.anchoredPosition = Vector2.zero;
            cancelRect.sizeDelta = new Vector2(260f, 55f);

            // 消息提示区域
            CreateMessageArea(canvas.transform);
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
                if (slotButtonImages[i] != null)
                {
                    bool isSelected = (i + 1) == selectedSlot;
                    slotButtonImages[i].color = isSelected
                        ? new Color(buttonColor.r * 1.3f, buttonColor.g * 1.3f, buttonColor.b * 1.3f, 1f)
                        : buttonColor;
                }
            }
        }

        void OnSaveClicked()
        {
            if (selectedSlot < 1)
            {
                ShowMessage("请先选择一个存档栏位！", Color.yellow);
                return;
            }

            if (SaveSystem.SaveSystem.HasSave(selectedSlot))
            {
                ConfirmDialogUI.Show($"存档槽 {selectedSlot} 已有存档，是否覆盖？",
                    onConfirm: () => DoSave(selectedSlot),
                    onCancel: null);
            }
            else
            {
                DoSave(selectedSlot);
            }
        }

        void DoSave(int slot)
        {
            var player = FindFirstObjectByType<PlatformerPlayerController>();
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;

            var data = new SaveSystem.SaveData
            {
                playerPosition = new SaveSystem.SerializableVector3(pos),
                playTimeSeconds = SaveSystem.GameTimer.Instance?.GetElapsedTime() ?? 0f,
                saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sceneName = SceneManager.GetActiveScene().name
            };

            SaveSystem.SaveSystem.Save(slot, data);
            GamePauseManager.Instance?.MarkProgressSaved();
            ShowMessage("保存成功！", Color.green);

            // 刷新槽位显示
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int s = i + 1;
                Text txt = slotButtons[i].GetComponentInChildren<Text>();
                if (txt != null)
                    txt.text = GetSlotLabel(s);
            }

            Invoke(nameof(Close), 1.5f);
        }

        void OnCancelClicked()
        {
            CancelInvoke(nameof(Close));
            GamePauseManager.Instance?.OnSaveGameUIClosed();
            Destroy(gameObject);
        }

        void CreateMessageArea(Transform canvasTransform)
        {
            GameObject msgGO = new GameObject("MessageText");
            msgGO.transform.SetParent(canvasTransform, false);

            messageText = msgGO.AddComponent<Text>();
            messageText.text = "";
            messageText.font = EffectiveFont;
            messageText.fontSize = 26;
            messageText.color = Color.green;
            messageText.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = msgGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.15f);
            rect.anchorMax = new Vector2(0.5f, 0.15f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 50f);

            messageObject = msgGO;
            msgGO.SetActive(false);
        }

        void ShowMessage(string msg, Color color)
        {
            if (messageText != null)
            {
                messageText.text = msg;
                messageText.color = color;
                messageObject.SetActive(true);
            }
        }

        void Close()
        {
            if (this != null && gameObject != null)
            {
                GamePauseManager.Instance?.OnSaveGameUIClosed();
                Destroy(gameObject);
            }
        }
    }
}
