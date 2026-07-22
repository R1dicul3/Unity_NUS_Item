using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 设置（Settings）界面 UI 构建器。
    /// 支持代码动态生成 UI 与 Prefab 加载两种模式。
    /// 提供主音量、BGM、SFX 三个滑块控制，数值实时同步到 GameSettings 并持久化。
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Appearance")]
        public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        public Color panelColor = new Color(0.12f, 0.12f, 0.15f, 1f);
        public Color titleColor = Color.white;
        public Color labelColor = Color.white;
        public Color sliderFillColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Font overrideFont;

        [Header("Layout")]
        [Range(24, 100)] public int titleFontSize = 56;
        [Range(16, 48)] public int labelFontSize = 28;
        [Range(16, 48)] public int valueFontSize = 24;
        [Range(16, 64)] public int backButtonFontSize = 24;
        [Range(200, 800)] public float panelWidth = 560f;
        [Range(30, 120)] public float sliderHeight = 70f;
        [Range(0, 60)] public float spacing = 24f;

        [Header("Prefab UI")]
        [SerializeField] private GameObject canvasPrefab;

        [Header("Scene Flow")]
        [Tooltip("如果为空，返回按钮将调用 SceneManager.LoadScene(PreviousScene)")]
        public string backSceneName = "";

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();
        private PlayerInputActions inputActions;
        private Slider masterSlider;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Text masterValueText;
        private Text bgmValueText;
        private Text sfxValueText;
        private bool isOverlayMode = false;
        private System.Action onCloseCallback;

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
            if (inputActions == null) return;
            inputActions.Player.Menu.performed -= OnMenuPerformed;
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            OnBackClicked();
        }

        /// <summary>
        /// 以叠加模式打开设置界面（不切换场景）。适合暂停菜单调用。
        /// </summary>
        public static SettingsUI ShowOverlay(System.Action onClose = null)
        {
            GameObject go = new GameObject("SettingsUI");
            SettingsUI ui = go.AddComponent<SettingsUI>();
            ui.isOverlayMode = true;
            ui.onCloseCallback = onClose;
            return ui;
        }

        void BuildUI()
        {
            if (TryBuildPrefabUI())
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 250;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);

            // 设置面板
            GameObject panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(canvas.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = panelColor;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(panelWidth + 80f, 560f);

            RectTransform content = CreateContent(panel.transform);

            // 标题
            MenuUIHelper.CreateText(content, "Settings", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 90f);

            // 间距
            GameObject spacer1 = new GameObject("Spacer");
            spacer1.transform.SetParent(content, false);
            RectTransform spacer1Rect = spacer1.AddComponent<RectTransform>();
            spacer1Rect.sizeDelta = new Vector2(panelWidth, 16f);
            LayoutElement spacer1Layout = spacer1.AddComponent<LayoutElement>();
            spacer1Layout.preferredWidth = panelWidth;
            spacer1Layout.preferredHeight = 16f;
            spacer1Layout.flexibleHeight = 0f;

            // 音量滑块
            float sliderWidth = panelWidth;
            masterSlider = CreateVolumeSlider(content, "Master Volume", sliderWidth, sliderHeight,
                GameSettings.Instance?.MasterVolume ?? 1f, OnMasterVolumeChanged, out masterValueText);
            bgmSlider = CreateVolumeSlider(content, "BGM Volume", sliderWidth, sliderHeight,
                GameSettings.Instance?.MusicVolume ?? 1f, OnBGMVolumeChanged, out bgmValueText);
            sfxSlider = CreateVolumeSlider(content, "SFX Volume", sliderWidth, sliderHeight,
                GameSettings.Instance?.SFXVolume ?? 1f, OnSFXVolumeChanged, out sfxValueText);

            // 间距
            GameObject spacer2 = new GameObject("Spacer");
            spacer2.transform.SetParent(content, false);
            RectTransform spacer2Rect = spacer2.AddComponent<RectTransform>();
            spacer2Rect.sizeDelta = new Vector2(panelWidth, 20f);
            LayoutElement spacer2Layout = spacer2.AddComponent<LayoutElement>();
            spacer2Layout.preferredWidth = panelWidth;
            spacer2Layout.preferredHeight = 20f;
            spacer2Layout.flexibleHeight = 0f;

            // 返回按钮
            Button backButton = MenuUIHelper.CreateButton(content, "< Back", backButtonFontSize, 55f,
                new Color(0.25f, 0.35f, 0.55f, 1f), OnBackClicked, EffectiveFont, true);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.sizeDelta = new Vector2(200f, 55f);

            SubscribeToSettings();
        }

        bool TryBuildPrefabUI()
        {
            GameObject prefab = canvasPrefab != null
                ? canvasPrefab
                : Resources.Load<GameObject>("UI/SettingsPanelCanvas");

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
                canvasComponent.sortingOrder = 250;
            }

            bool hasRequiredControls = true;

            hasRequiredControls &= TryBindSlider(canvas.transform, "MasterVolumeSlider", OnMasterVolumeChanged, out masterSlider, out masterValueText);
            hasRequiredControls &= TryBindSlider(canvas.transform, "BGMVolumeSlider", OnBGMVolumeChanged, out bgmSlider, out bgmValueText);
            hasRequiredControls &= TryBindSlider(canvas.transform, "SFXVolumeSlider", OnSFXVolumeChanged, out sfxSlider, out sfxValueText);
            hasRequiredControls &= MenuUIHelper.TryBindButton(canvas.transform, "BackButton", OnBackClicked, out _);

            if (!hasRequiredControls)
            {
                Debug.LogWarning("[SettingsUI] Settings prefab is missing expected controls. Falling back to generated UI.");
                Destroy(canvas);
                return false;
            }

            // 初始化滑块值为当前设置
            if (GameSettings.Instance != null)
            {
                if (masterSlider != null) masterSlider.value = GameSettings.Instance.MasterVolume;
                if (bgmSlider != null) bgmSlider.value = GameSettings.Instance.MusicVolume;
                if (sfxSlider != null) sfxSlider.value = GameSettings.Instance.SFXVolume;
            }

            UpdateValueTexts();
            SubscribeToSettings();
            return true;
        }

        static RectTransform CreateContent(Transform parent)
        {
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(parent, false);
            RectTransform rect = contentGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(40f, 40f);
            rect.offsetMax = new Vector2(-40f, -40f);

            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        Slider CreateVolumeSlider(Transform parent, string label, float width, float height,
            float defaultValue, UnityEngine.Events.UnityAction<float> onValueChanged, out Text valueText)
        {
            GameObject rowGO = new GameObject(label + " Row");
            rowGO.transform.SetParent(parent, false);
            RectTransform rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(width, height);

            LayoutElement rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = width;
            rowLayout.preferredHeight = height;
            rowLayout.flexibleHeight = 0f;

            // Label + Value（绝对定位在 row 顶部）
            GameObject labelRowGO = new GameObject("LabelRow");
            labelRowGO.transform.SetParent(rowGO.transform, false);
            RectTransform labelRowRect = labelRowGO.GetComponent<RectTransform>();
            labelRowRect.anchorMin = new Vector2(0f, 1f);
            labelRowRect.anchorMax = new Vector2(1f, 1f);
            labelRowRect.pivot = new Vector2(0.5f, 1f);
            labelRowRect.anchoredPosition = Vector2.zero;
            labelRowRect.sizeDelta = new Vector2(0f, 28f);

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(labelRowGO.transform, false);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = EffectiveFont;
            labelText.fontSize = labelFontSize;
            labelText.color = labelColor;
            labelText.alignment = TextAnchor.MiddleLeft;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-60f, 0f);

            // Value text
            GameObject valueGO = new GameObject("ValueText");
            valueGO.transform.SetParent(labelRowGO.transform, false);
            Text valText = valueGO.AddComponent<Text>();
            valText.text = Mathf.RoundToInt(defaultValue * 100f) + "%";
            valText.font = EffectiveFont;
            valText.fontSize = valueFontSize;
            valText.color = labelColor;
            valText.alignment = TextAnchor.MiddleRight;

            RectTransform valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(60f, 0f);

            valueText = valText;

            // Slider（绝对定位在 row 底部）
            Slider slider = MenuUIHelper.CreateSlider(rowGO.transform, "", width, height - 32f,
                0f, 1f, defaultValue, onValueChanged, EffectiveFont, labelColor);

            RectTransform sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = new Vector2(0f, 2f);
            sliderRect.sizeDelta = new Vector2(0f, height - 34f);

            // 自定义 Slider 填充颜色
            if (slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = sliderFillColor;
                }
            }

            return slider;
        }

        static bool TryBindSlider(Transform root, string sliderName,
            UnityEngine.Events.UnityAction<float> onValueChanged, out Slider slider, out Text valueText)
        {
            slider = null;
            valueText = null;

            Transform sliderTransform = MenuUIHelper.FindChildRecursive(root, sliderName);
            if (sliderTransform == null)
            {
                Debug.LogWarning($"[SettingsUI] Missing prefab slider: {sliderName}");
                return false;
            }

            slider = sliderTransform.GetComponent<Slider>();
            if (slider == null)
            {
                Debug.LogWarning($"[SettingsUI] Prefab child has no Slider component: {sliderName}");
                return false;
            }

            slider.onValueChanged.RemoveAllListeners();
            if (onValueChanged != null)
                slider.onValueChanged.AddListener(onValueChanged);

            // 尝试查找 ValueText（可能在 Slider 内部，也可能在同级 LabelRow 中）
            Transform valueTransform = MenuUIHelper.FindChildRecursive(sliderTransform, "ValueText");
            if (valueTransform == null && sliderTransform.parent != null)
            {
                valueTransform = MenuUIHelper.FindChildRecursive(sliderTransform.parent, "ValueText");
            }
            if (valueTransform != null)
            {
                valueText = valueTransform.GetComponent<Text>();
            }

            return true;
        }

        #region Volume Handlers

        void OnMasterVolumeChanged(float value)
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.MasterVolume = value;
            }
            UpdateValueTexts();
        }

        void OnBGMVolumeChanged(float value)
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.MusicVolume = value;
            }
            UpdateValueTexts();
        }

        void OnSFXVolumeChanged(float value)
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.SFXVolume = value;
            }
            UpdateValueTexts();
            // 播放测试音效，让用户感知音量变化
            if (value > 0.001f)
            {
                AudioManager.Instance?.PlayOneShot(SoundType.UIHover);
            }
        }

        void UpdateValueTexts()
        {
            if (masterValueText != null && masterSlider != null)
                masterValueText.text = Mathf.RoundToInt(masterSlider.value * 100f) + "%";
            if (bgmValueText != null && bgmSlider != null)
                bgmValueText.text = Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
            if (sfxValueText != null && sfxSlider != null)
                sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        }

        #endregion

        #region Settings Events

        private void SubscribeToSettings()
        {
            if (GameSettings.Instance == null) return;
            GameSettings.Instance.OnMasterVolumeChanged += OnExternalMasterChanged;
            GameSettings.Instance.OnMusicVolumeChanged += OnExternalMusicChanged;
            GameSettings.Instance.OnSFXVolumeChanged += OnExternalSFXChanged;
        }

        private void UnsubscribeFromSettings()
        {
            if (GameSettings.Instance == null) return;
            GameSettings.Instance.OnMasterVolumeChanged -= OnExternalMasterChanged;
            GameSettings.Instance.OnMusicVolumeChanged -= OnExternalMusicChanged;
            GameSettings.Instance.OnSFXVolumeChanged -= OnExternalSFXChanged;
        }

        private void OnExternalMasterChanged(float value)
        {
            if (masterSlider != null) masterSlider.value = value;
            UpdateValueTexts();
        }

        private void OnExternalMusicChanged(float value)
        {
            if (bgmSlider != null) bgmSlider.value = value;
            UpdateValueTexts();
        }

        private void OnExternalSFXChanged(float value)
        {
            if (sfxSlider != null) sfxSlider.value = value;
            UpdateValueTexts();
        }

        #endregion

        void OnBackClicked()
        {
            UnsubscribeFromSettings();
            onCloseCallback?.Invoke();

            if (isOverlayMode)
            {
                Destroy(gameObject);
            }
            else if (!string.IsNullOrEmpty(backSceneName))
            {
                SceneManager.LoadScene(backSceneName);
            }
            else
            {
                // 默认返回 MainMenu
                SceneManager.LoadScene("MainMenu");
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromSettings();
        }
    }
}
