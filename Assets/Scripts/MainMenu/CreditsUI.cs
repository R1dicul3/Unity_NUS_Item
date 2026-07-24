using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// Credits（制作人名单）界面 UI 构建器。
    /// 居中显示标题与制作人员名单，底部提供返回主菜单按钮。
    /// 可在 Inspector 中自定义 credits 数组内容。
    /// </summary>
    public class CreditsUI : MonoBehaviour
    {
        [System.Serializable]
        public struct CreditEntry
        {
            [Tooltip("职位/角色，例如 Game Design")]
            public string role;
            [Tooltip("姓名")]
            public string name;
        }

        [Header("内容")]
        public CreditEntry[] credits = new CreditEntry[]
        {
            new CreditEntry { role = "Game Design", name = "Your Name" },
            new CreditEntry { role = "Programming", name = "Your Name" },
            new CreditEntry { role = "Art", name = "Your Name" },
            new CreditEntry { role = "Music & Sound", name = "Your Name" },
            new CreditEntry { role = "Special Thanks", name = "Unity Community" }
        };

        [Header("外观设置")]
        public Color backgroundColor = Color.white;
        public Color titleColor = Color.black;
        public Color roleColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        public Color nameColor = Color.black;
        public Font overrideFont;

        [Header("尺寸设置")]
        [Range(24, 100)]
        public int titleFontSize = 56;

        [Range(16, 64)]
        public int nameFontSize = 32;

        [Range(12, 48)]
        public int roleFontSize = 24;

        [Range(16, 64)]
        public int backButtonFontSize = 24;

        [Range(200, 800)]
        public float contentWidth = 600f;

        [Range(0, 60)]
        public float entrySpacing = 30f;

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();

        void Awake()
        {
            BuildUI();
        }

        void BuildUI()
        {
            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, contentWidth, entrySpacing);

            // 标题
            MenuUIHelper.CreateText(content, "Credits", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 100f);

            // 人员名单
            foreach (var entry in credits)
            {
                CreateCreditEntry(content, entry);
            }

            // 间距
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(contentWidth, 30f);

            // 返回按钮
            MenuUIHelper.CreateButton(content, "< Back", backButtonFontSize, 50f,
                MenuUIHelper.DefaultButtonColor, OnBackClicked, EffectiveFont, true);
        }

        bool TryBuildPrefabUI()
        {
            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/CreditsCanvas");

            if (prefab == null)
            {
                return false;
            }

            GameObject canvas = Instantiate(prefab);
            canvas.name = prefab.name;
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();

            bool hasRequiredControls = MenuUIHelper.TryBindButton(canvas.transform, "BackButton", OnBackClicked, out _);

            // 查找内容容器，动态生成 credits 条目
            Transform content = MenuUIHelper.FindChildRecursive(canvas.transform, "CreditsContent");
            if (content != null)
            {
                foreach (var entry in credits)
                {
                    CreateCreditEntry(content.GetComponent<RectTransform>(), entry);
                }
            }

            if (!hasRequiredControls)
            {
                Debug.LogWarning("[CreditsUI] Credits prefab is missing expected controls. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            return true;
        }

        void OnBackClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void CreateCreditEntry(RectTransform parent, CreditEntry entry)
        {
            GameObject entryGO = new GameObject($"Credit_{entry.role}");
            entryGO.transform.SetParent(parent, false);
            RectTransform entryRect = entryGO.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(contentWidth, 80f);

            VerticalLayoutGroup entryVlg = entryGO.AddComponent<VerticalLayoutGroup>();
            entryVlg.childAlignment = TextAnchor.MiddleCenter;
            entryVlg.spacing = 5f;
            entryVlg.childControlWidth = true;
            entryVlg.childControlHeight = false;
            entryVlg.childForceExpandWidth = true;
            entryVlg.childForceExpandHeight = false;

            // Role
            MenuUIHelper.CreateText(entryGO.transform, entry.role, roleFontSize, roleColor,
                EffectiveFont, FontStyle.Italic, 30f);

            // Name
            MenuUIHelper.CreateText(entryGO.transform, entry.name, nameFontSize, nameColor,
                EffectiveFont, FontStyle.Normal, 40f);
        }
    }
}
