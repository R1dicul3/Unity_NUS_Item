using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MainMenu
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Appearance")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Color titleColor = Color.white;
        public Font overrideFont;

        [Header("Layout")]
        [Range(30, 150)] public int titleFontSize = 72;
        [Range(16, 64)] public int buttonFontSize = 32;
        [Range(200, 600)] public float buttonWidth = 300f;
        [Range(30, 120)] public float buttonHeight = 60f;
        [Range(0, 60)] public float spacing = 20f;

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private GameObject messageObject;
        private Transform messageRoot;
        private Graphic messageGraphic;
        private PlayerInputActions inputActions;

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

            GamePauseManager.Instance?.ResumeGame();
        }

        private void BuildUI()
        {
            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 100;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, buttonWidth, spacing);

            MenuUIHelper.CreateText(content, "Stopover", titleFontSize, titleColor, EffectiveFont, FontStyle.Bold, 120f);
            CreateMenuButton(content, "Resume Game", OnResumeGame, true);
            CreateMenuButton(content, "Load Game", OnLoadGame, true);
            CreateMenuButton(content, "Save Game", OnSaveGame, true);
            CreateMenuButton(content, "Settings", OnSettings, true);
            CreateMenuButton(content, "Return to Main Menu", OnReturnToMainMenu, true);
            CreateMessageArea(canvas.transform);
        }

        private bool TryBuildPrefabUI()
        {
            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/PauseMenuCanvas");

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
                canvasComponent.sortingOrder = 100;
            }

            bool hasRequiredControls =
                MenuUIHelper.TryBindButton(canvas.transform, "ResumeButton", OnResumeGame, out _)
                & MenuUIHelper.TryBindButton(canvas.transform, "LoadButton", OnLoadGame, out _)
                & MenuUIHelper.TryBindButton(canvas.transform, "SaveButton", OnSaveGame, out _)
                & MenuUIHelper.TryBindButton(canvas.transform, "SettingsButton", OnSettings, out _)
                & MenuUIHelper.TryBindButton(canvas.transform, "MainMenuButton", OnReturnToMainMenu, out _);

            BindMessage(canvas.transform);

            if (!hasRequiredControls)
            {
                Debug.LogWarning("[PauseMenuUI] Pause prefab is missing expected controls. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            return true;
        }

        private void CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, bool isImplemented)
        {
            MenuUIHelper.CreateButton(parent, label, buttonFontSize, buttonHeight,
                buttonColor, isImplemented ? onClick : () => OnPlaceholderClicked(label), EffectiveFont, true);
        }

        private void OnPlaceholderClicked(string label)
        {
            Debug.Log($"[{label}] not implemented.");
        }

        private void OnResumeGame()
        {
            GamePauseManager.Instance?.ResumeGame();
        }

        private void OnLoadGame()
        {
            GamePauseManager.Instance?.LoadGame();
        }

        private void OnSaveGame()
        {
            GamePauseManager.Instance?.SaveGame();
        }

        private void OnSettings()
        {
            GamePauseManager.Instance?.OpenSettings();
        }

        private void OnReturnToMainMenu()
        {
            GamePauseManager.Instance?.ReturnToMainMenu();
        }

        private void CreateMessageArea(Transform canvasTransform)
        {
            GameObject message = new GameObject("MessageText");
            message.transform.SetParent(canvasTransform, false);

            Text text = message.AddComponent<Text>();
            text.text = "";
            text.font = EffectiveFont;
            text.fontSize = 28;
            text.color = Color.green;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = message.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600f, 50f);

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

        public void ShowSaveSuccess()
        {
            if (messageRoot == null)
            {
                return;
            }

            MenuUIHelper.TrySetText(messageRoot, "Saved.");
            if (messageGraphic != null)
            {
                messageGraphic.color = Color.green;
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
    }
}
