using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MainMenu
{
    /// <summary>
    /// 结束界面 UI 构建器。
    /// 加载 EndingCanvas Prefab 并配置分页展示，所有页展示完后自动跳转至 Credits 场景。
    /// 支持 Prefab 模式与代码动态生成模式双轨回退。
    /// </summary>
    public class EndingUI : MonoBehaviour
    {
        [Header("Prefab UI")]
        [Tooltip("EndingCanvas Prefab（留空则从 Resources/UI/EndingCanvas 自动加载）")]
        [SerializeField] private GameObject canvasPrefab;

        [Header("跳转设置")]
        [Tooltip("所有页展示完后跳转的目标场景名")]
        [SerializeField] private string nextSceneName = "Credits";

        [Header("外观设置（仅代码生成模式生效）")]
        public Color backgroundColor = new Color(0.08f, 0.12f, 0.17f, 0.95f);

        void Awake()
        {
            BuildUI();
        }

        void BuildUI()
        {
            if (TryBuildPrefabUI())
            {
                AudioManager.Instance?.PlayMusic(SoundType.EndingMusic);
                return;
            }

            // Fallback: 代码动态生成基础 UI
            Canvas canvas = MenuUIHelper.CreateCanvas();
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);

            // 创建 PagesContainer
            GameObject pagesContainerGO = new GameObject("PagesContainer");
            pagesContainerGO.transform.SetParent(canvas.transform, false);
            RectTransform pagesRect = pagesContainerGO.AddComponent<RectTransform>();
            pagesRect.anchorMin = Vector2.zero;
            pagesRect.anchorMax = Vector2.one;
            pagesRect.offsetMin = Vector2.zero;
            pagesRect.offsetMax = Vector2.zero;

            // 创建示例 Page 1
            GameObject page1 = CreatePage(pagesContainerGO.transform, "Page 1");
            CreateLine(page1.transform, "Thank you for playing.");
            CreateLine(page1.transform, "Your journey has come to an end.");

            // 创建示例 Page 2
            GameObject page2 = CreatePage(pagesContainerGO.transform, "Page 2");
            CreateLine(page2.transform, "But every ending is a new beginning.");
            CreateLine(page2.transform, "We hope to see you again.");

            // SkipPrompt
            GameObject skipPromptGO = CreateSkipPrompt(canvas.transform);

            // 挂载 EndingCarousel
            EndingCarousel carousel = pagesContainerGO.AddComponent<EndingCarousel>();
            carousel.pages = new EndingPage[]
            {
                new EndingPage { content = page1, displayMode = DisplayMode.FadeAll, displayDuration = 3f },
                new EndingPage { content = page2, displayMode = DisplayMode.TypewriterLines, displayDuration = 0.5f }
            };
            carousel.skipPrompt = skipPromptGO;
            carousel.OnAllPagesShown += OnAllPagesShown;

            AudioManager.Instance?.PlayMusic(SoundType.EndingMusic);
        }

        bool TryBuildPrefabUI()
        {
            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/EndingCanvas");

            if (prefab == null)
            {
                return false;
            }

            GameObject canvas = Instantiate(prefab);
            canvas.name = prefab.name;
            MenuUIHelper.EnsureCamera();
            MenuUIHelper.EnsureEventSystem();

            // 查找 EndingCarousel
            EndingCarousel carousel = canvas.GetComponentInChildren<EndingCarousel>(true);
            if (carousel == null)
            {
                Debug.LogWarning("[EndingUI] EndingCanvas prefab is missing EndingCarousel component. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            carousel.OnAllPagesShown += OnAllPagesShown;

            return true;
        }

        void OnAllPagesShown()
        {
            SceneManager.LoadScene(nextSceneName);
        }

        #region Fallback Helpers

        static GameObject CreatePage(Transform parent, string name)
        {
            GameObject pageGO = new GameObject(name);
            pageGO.transform.SetParent(parent, false);
            RectTransform rect = pageGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 400f);
            return pageGO;
        }

        static void CreateLine(Transform parent, string text)
        {
            GameObject lineGO = new GameObject("Line", typeof(TextMeshProUGUI));
            lineGO.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = lineGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            RectTransform rect = lineGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800f, 50f);
        }

        static GameObject CreateSkipPrompt(Transform parent)
        {
            GameObject skipPromptGO = new GameObject("SkipPrompt", typeof(TextMeshProUGUI));
            skipPromptGO.transform.SetParent(parent, false);
            TextMeshProUGUI skipText = skipPromptGO.GetComponent<TextMeshProUGUI>();
            skipText.text = "Press any key to skip";
            skipText.fontSize = 18;
            skipText.color = new Color(1f, 1f, 1f, 0.5f);
            skipText.alignment = TextAlignmentOptions.Center;
            skipText.raycastTarget = false;

            RectTransform skipRect = skipPromptGO.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0f);
            skipRect.anchorMax = new Vector2(0.5f, 0f);
            skipRect.pivot = new Vector2(0.5f, 0f);
            skipRect.anchoredPosition = new Vector2(0f, 40f);
            skipRect.sizeDelta = new Vector2(400f, 40f);

            return skipPromptGO;
        }

        #endregion
    }
}
