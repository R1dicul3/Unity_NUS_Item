using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 暂停界面 UI 构建器。
    /// 由 GamePauseManager 在游戏运行时动态创建，风格与主菜单保持一致。
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("外观设置")]
        [Tooltip("背景颜色")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

        [Tooltip("按钮颜色")]
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);

        [Tooltip("标题文字颜色")]
        public Color titleColor = Color.white;

        [Tooltip("全局字体覆盖（留空则使用内置默认字体）")]
        public Font overrideFont;

        [Header("尺寸设置")]
        [Range(30, 150)]
        public int titleFontSize = 72;

        [Range(16, 64)]
        public int buttonFontSize = 32;

        [Range(200, 600)]
        public float buttonWidth = 300f;

        [Range(30, 120)]
        public float buttonHeight = 60f;

        [Range(0, 60)]
        public float spacing = 20f;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private GameObject messageObject;
        private Text messageText;
        private PlayerInputActions inputActions;

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
            GamePauseManager.Instance?.ResumeGame();
        }

        void BuildUI()
        {
            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 100;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, buttonWidth, spacing);

            // 标题
            MenuUIHelper.CreateText(content, "Stopover", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 120f);

            // 按钮：Resume Game
            CreateMenuButton(content, "Resume Game", OnResumeGame, true);

            // 按钮：Load Game
            CreateMenuButton(content, "Load Game", OnLoadGame, true);

            // 按钮：Save Game
            CreateMenuButton(content, "Save Game", OnSaveGame, true);

            // 按钮：Settings（暂未实现）
            CreateMenuButton(content, "Settings", OnSettings, false);

            // 按钮：Return to Main Menu
            CreateMenuButton(content, "Return to Main Menu", OnReturnToMainMenu, true);

            // 消息提示区域
            CreateMessageArea(canvas.transform);
        }

        void CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, bool isImplemented)
        {
            MenuUIHelper.CreateButton(parent, label, buttonFontSize, buttonHeight,
                buttonColor, isImplemented ? onClick : () => OnPlaceholderClicked(label), EffectiveFont, true);
        }

        void OnPlaceholderClicked(string label)
        {
            Debug.Log($"[{label}] 功能暂未实现。");
        }

        void OnResumeGame()
        {
            GamePauseManager.Instance?.ResumeGame();
        }

        void OnLoadGame()
        {
            GamePauseManager.Instance?.LoadGame();
        }

        void OnSaveGame()
        {
            GamePauseManager.Instance?.SaveGame();
        }

        void OnSettings()
        {
            Debug.Log("Settings clicked - 暂未实现。");
        }

        void OnReturnToMainMenu()
        {
            GamePauseManager.Instance?.ReturnToMainMenu();
        }

        void CreateMessageArea(Transform canvasTransform)
        {
            GameObject msgGO = new GameObject("MessageText");
            msgGO.transform.SetParent(canvasTransform, false);

            messageText = msgGO.AddComponent<Text>();
            messageText.text = "";
            messageText.font = EffectiveFont;
            messageText.fontSize = 28;
            messageText.color = Color.green;
            messageText.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = msgGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600f, 50f);

            messageObject = msgGO;
            msgGO.SetActive(false);
        }

        public void ShowSaveSuccess()
        {
            if (messageText != null)
            {
                messageText.text = "保存成功！";
                messageObject.SetActive(true);
            }
        }
    }
}
