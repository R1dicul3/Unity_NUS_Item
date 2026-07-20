using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 主菜单 UI 构建器。
    /// 将本脚本挂载到场景中的任意空 GameObject 上，运行后会自动生成居中排列的菜单界面：
    /// Logo（Stopover）→ New Game → Load Game → Settings → Credits → Exit。
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("外观设置")]
        [Tooltip("背景颜色")]
        public Color backgroundColor = Color.white;

        [Tooltip("按钮颜色")]
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);

        [Tooltip("Logo 文字颜色")]
        public Color logoColor = Color.black;

        [Tooltip("全局字体覆盖（留空则使用内置默认字体）")]
        public Font overrideFont;

        [Header("尺寸设置")]
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

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();

        void Awake()
        {
            BuildUI();
        }

        void BuildUI()
        {
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
            CreateMenuButton(content, "Settings", OnSettings, false);

            // 按钮：Credits
            CreateMenuButton(content, "Credits", OnCredits, true);

            // 按钮：Exit
            CreateMenuButton(content, "Exit", OnExit, true);
        }

        void CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, bool isImplemented)
        {
            // 所有按钮统一显示为可交互状态，颜色一致；未实现的功能点击后打印日志
            MenuUIHelper.CreateButton(parent, label, buttonFontSize, buttonHeight,
                buttonColor, isImplemented ? onClick : () => OnPlaceholderClicked(label), EffectiveFont, true);
        }

        void OnPlaceholderClicked(string label)
        {
            Debug.Log($"[{label}] 功能暂未实现。");
        }

        void OnNewGame()
        {
            if (SaveSystem.SaveSystem.IsFull())
            {
                ConfirmDialogUI.Show(
                    "存档栏位已满。开始新游戏后，若进行保存将会覆盖最早的存档。是否继续？",
                    onConfirm: () =>
                    {
                        GamePauseManager.Instance?.StartNewGame();
                    },
                    onCancel: () => SceneManager.LoadScene("LoadGame"));
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

        void OnSettings()
        {
            Debug.Log("Settings clicked - 暂未实现。");
        }

        void OnCredits()
        {
            SceneManager.LoadScene("Credits");
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
