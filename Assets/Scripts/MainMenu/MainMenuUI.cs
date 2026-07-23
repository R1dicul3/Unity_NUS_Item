using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// Main Menu UI builder.
    /// Attach this script to any empty GameObject in the scene to auto-generate a centered menu:
    /// Logo (Stopover) → New Game → Load Game → Settings → Credits → Exit.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Appearance")]
        [Tooltip("Background color")]
        public Color backgroundColor = Color.white;

        [Tooltip("Button color")]
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);

        [Tooltip("Logo text color")]
        public Color logoColor = Color.black;

        [Tooltip("Global font override (leave empty to use built-in default)")]
        public Font overrideFont;

        [Header("Layout")]
        [Range(30, 150)]
        public int logoFontSize = 72;

        [Range(16, 64)]
        public int buttonFontSize = 32;

        [Range(200, 600)]
        public float buttonWidth = 300f;

        [Range(30, 120)]
        public float buttonHeight = 60f;

        [Range(0, 60)]
        public float spacing = 20f;

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();

        void Awake()
        {
            BuildUI();
        }

        void Start()
        {
            AudioManager.Instance?.PlayMusic(SoundType.MainMenuMusic);
        }

        void BuildUI()
        {
            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, buttonWidth, spacing);

            // Logo
            MenuUIHelper.CreateText(content, "Stopover", logoFontSize, logoColor,
                EffectiveFont, FontStyle.Bold, 120f);

            // 按钮：New Game
            CreateMenuButton(content, "New Game", OnNewGame, true);

            // 按钮：Load Game
            CreateMenuButton(content, "Load Game", OnLoadGame, true);

            // 按钮：Settings（暂未实现）
            CreateMenuButton(content, "Settings", null, false);

            // 按钮：Credits
            CreateMenuButton(content, "Credits", OnCredits, true);

            // 按钮：Exit
            CreateMenuButton(content, "Exit", OnExit, true);
        }

        bool TryBuildPrefabUI()
        {
            if (canvasPrefab == null)
            {
                return false;
            }

            GameObject canvas = Instantiate(canvasPrefab);
            canvas.name = canvasPrefab.name;
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();

            bool hasRequiredButtons =
                TryBindButton(canvas.transform, "NewGameButton", OnNewGame)
                & TryBindButton(canvas.transform, "LoadGameButton", OnLoadGame)
                & TryBindButton(canvas.transform, "SettingsButton", OnSettings)
                & TryBindButton(canvas.transform, "CreditsButton", OnCredits)
                & TryBindButton(canvas.transform, "ExitButton", OnExit);

            if (!hasRequiredButtons)
            {
                Debug.LogWarning("[MainMenuUI] Main menu prefab is missing one or more expected buttons. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            return true;
        }

        bool TryBindButton(Transform root, string buttonName, UnityEngine.Events.UnityAction onClick)
        {
            Transform buttonTransform = FindChildRecursive(root, buttonName);
            if (buttonTransform == null)
            {
                Debug.LogWarning($"[MainMenuUI] Missing prefab child: {buttonName}");
                return false;
            }

            Button button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[MainMenuUI] Prefab child has no Button component: {buttonName}");
                return false;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            // 自动挂载按钮音效组件（悬停 + 选中 + 点击）
            if (button.GetComponent<UIButtonSound>() == null)
            {
                button.gameObject.AddComponent<UIButtonSound>();
            }

            return true;
        }

        static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        void CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, bool isImplemented)
        {
            // 所有按钮统一显示为可交互状态，颜色一致；未实现的功能点击后打印日志
            MenuUIHelper.CreateButton(parent, label, buttonFontSize, buttonHeight,
                buttonColor, isImplemented ? onClick : () => OnPlaceholderClicked(label), EffectiveFont, true);
        }

        void OnPlaceholderClicked(string label)
        {
            Debug.Log($"[{label}] Feature not yet implemented.");
        }

        void OnNewGame()
        {
            if (SaveSystem.SaveSystem.IsFull())
            {
                ConfirmDialogUI.Show(
                    "Save slots are full. Starting a new game will overwrite the oldest save slot. Continue?",
                    onConfirm: () =>
                    {
                        GamePauseManager.Instance?.StartNewGame();
                    },
                    onCancel: () => SceneManager.LoadScene("LoadGame"),
                    dialogSound: SoundType.UIAlert);
            }
            else
            {
                GamePauseManager.Instance?.StartNewGame();
            }
        }

        void OnLoadGame()
        {
            SceneManager.LoadScene("LoadGame");
        }

        void OnCredits()
        {
            SceneManager.LoadScene("Credits");
        }

        void OnSettings()
        {
            Debug.Log("[Settings] 功能暂未实现。");
        }

        void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
