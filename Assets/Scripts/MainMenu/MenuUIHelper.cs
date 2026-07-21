using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MainMenu
{
    /// <summary>
    /// 通用 UI 构建辅助类，为主菜单、读档界面和 Credits 提供统一的基础构建方法。
    /// </summary>
    public static class MenuUIHelper
    {
        // 默认配色（可通过各 UI 脚本 Inspector 覆盖）
        public static readonly Color DefaultBackgroundColor = Color.white;
        public static readonly Color DefaultButtonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public static readonly Color DefaultTitleColor = Color.black;
        public static readonly Color DefaultTextColor = Color.black;

        /// <summary>
        /// 创建 ScreenSpaceOverlay Canvas，并配置 CanvasScaler 与 GraphicRaycaster。
        /// </summary>
        public static Canvas CreateCanvas()
        {
            EnsureCamera(); // 消除 "No cameras rendering" 提示

            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// 如果场景中不存在 Camera，则自动创建一个极简 Camera，用于消除 Game 视图中的
        /// "Display 1 No cameras rendering" 提示。该 Camera 不影响 UI 渲染。
        /// </summary>
        public static void EnsureCamera()
        {
#if UNITY_2023_1_OR_NEWER
            var existingCam = UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
#else
            var existingCam = UnityEngine.Object.FindObjectOfType<Camera>(true);
#endif
            if (existingCam != null)
            {
                if (!existingCam.gameObject.activeInHierarchy)
                {
                    existingCam.gameObject.SetActive(true);
                }
                if (!existingCam.enabled)
                {
                    existingCam.enabled = true;
                }
                return;
            }

            GameObject camGO = new GameObject("Main Camera");
            Camera camera = camGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.depth = -1;
        }

        /// <summary>
        /// 如果场景中不存在 EventSystem，则自动创建一个。
        /// 会根据项目是否启用新 Input System 自动挂载对应的 Input Module。
        /// </summary>
        public static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
#else
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
#endif
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        /// <summary>
        /// 创建全屏背景 Image。
        /// </summary>
        public static Image CreateFullScreenBackground(Transform parent, Color color)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(parent, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = color;
            RectTransform rect = bgGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return bgImage;
        }

        /// <summary>
        /// 创建居中垂直布局的内容容器。
        /// </summary>
        public static RectTransform CreateCenteredContent(Transform parent, float width, float spacing = 20f)
        {
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(parent, false);
            RectTransform rect = contentGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 600f);

            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = spacing;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        /// <summary>
        /// 创建 Text 组件。
        /// </summary>
        public static Text CreateText(Transform parent, string text, int fontSize, Color color,
            Font font = null, FontStyle style = FontStyle.Normal, float height = 50f)
        {
            GameObject go = new GameObject(text);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = font ?? GetDefaultFont();
            if (txt.font == null)
            {
                Debug.LogError($"[MenuUIHelper] 无法为文本 \"{text}\" 获取字体。请在对应 UI 脚本的 Inspector 中设置 Override Font。");
            }
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = style;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            return txt;
        }

        /// <summary>
        /// 创建按钮，支持可用/禁用状态与点击回调。
        /// </summary>
        public static Button CreateButton(Transform parent, string label, int fontSize, float height,
            Color buttonColor, UnityEngine.Events.UnityAction onClick, Font font = null, bool interactable = true)
        {
            GameObject btnGO = new GameObject(label + " Button");
            btnGO.transform.SetParent(parent, false);

            Button btn = btnGO.AddComponent<Button>();
            btn.interactable = interactable;

            // 禁用状态的按钮颜色不要压得太暗，保证在背景上仍可见
            Color disabledImageColor = new Color(buttonColor.r * 0.55f, buttonColor.g * 0.55f, buttonColor.b * 0.55f, 0.65f);

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = interactable ? buttonColor : disabledImageColor;
            btn.targetGraphic = btnImage;

            if (onClick != null)
            {
                btn.onClick.AddListener(() => AudioManager.Instance?.PlayOneShot(SoundType.UIClick));
                btn.onClick.AddListener(onClick);
            }

            ColorBlock colors = btn.colors;
            colors.normalColor = interactable ? buttonColor : disabledImageColor;
            colors.highlightedColor = new Color(buttonColor.r * 1.2f, buttonColor.g * 1.2f, buttonColor.b * 1.2f, 1f);
            colors.pressedColor = new Color(buttonColor.r * 0.8f, buttonColor.g * 0.8f, buttonColor.b * 0.8f, 1f);
            colors.disabledColor = new Color(buttonColor.r * 0.4f, buttonColor.g * 0.4f, buttonColor.b * 0.4f, 0.6f);
            btn.colors = colors;

            RectTransform btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0f, height);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            Text btnText = textGO.AddComponent<Text>();
            btnText.text = label;
            btnText.font = font ?? GetDefaultFont();
            if (btnText.font == null)
            {
                Debug.LogError($"[MenuUIHelper] 无法为按钮 \"{label}\" 获取字体。请在对应 UI 脚本的 Inspector 中设置 Override Font。");
            }
            btnText.fontSize = fontSize;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        public static Transform FindChildRecursive(Transform root, string childName)
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

        public static bool TryBindButton(Transform root, string buttonName, UnityEngine.Events.UnityAction onClick, out Button button)
        {
            button = null;
            Transform buttonTransform = FindChildRecursive(root, buttonName);
            if (buttonTransform == null)
            {
                Debug.LogWarning($"[MenuUIHelper] Missing prefab child: {buttonName}");
                return false;
            }

            button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[MenuUIHelper] Prefab child has no Button component: {buttonName}");
                return false;
            }

            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(() => AudioManager.Instance?.PlayOneShot(SoundType.UIClick));
                button.onClick.AddListener(onClick);
            }

            return true;
        }

        public static bool TrySetText(Transform root, string objectName, string value)
        {
            Transform textTransform = FindChildRecursive(root, objectName);
            if (textTransform == null)
            {
                return false;
            }

            return TrySetText(textTransform, value);
        }

        public static bool TrySetText(Transform textRoot, string value)
        {
            Text legacyText = textRoot.GetComponent<Text>();
            if (legacyText != null)
            {
                legacyText.text = value;
                return true;
            }

            TextMeshProUGUI tmpText = textRoot.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = value;
                return true;
            }

            legacyText = textRoot.GetComponentInChildren<Text>(true);
            if (legacyText != null)
            {
                legacyText.text = value;
                return true;
            }

            tmpText = textRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                tmpText.text = value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 创建 Slider 组件，包含标签、填充条和滑块。
        /// 标签嵌套在 slider 内部上方，通过显式 RectTransform 定位。
        /// </summary>
        public static Slider CreateSlider(Transform parent, string label, float width, float height,
            float minValue, float maxValue, float defaultValue,
            UnityEngine.Events.UnityAction<float> onValueChanged, Font font = null, Color? labelColor = null)
        {
            Color textColor = labelColor ?? Color.white;

            // Slider 根对象
            GameObject sliderGO = new GameObject(label + " Slider");
            sliderGO.transform.SetParent(parent, false);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            slider.direction = Slider.Direction.LeftToRight;

            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(width, height);

            LayoutElement layoutElement = sliderGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;

            // Label（在 Slider 上方）
            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(sliderGO.transform, false);
                Text labelText = labelGO.AddComponent<Text>();
                labelText.text = label;
                labelText.font = font ?? GetDefaultFont();
                if (labelText.font == null)
                {
                    Debug.LogError($"[MenuUIHelper] 无法为 Slider 标签 \"{label}\" 获取字体。");
                }
                labelText.fontSize = 24;
                labelText.color = textColor;
                labelText.alignment = TextAnchor.MiddleLeft;

                RectTransform labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(0f, 28f);
            }

            // Slider 实际区域（Label 下方）
            GameObject trackGO = new GameObject("Track");
            trackGO.transform.SetParent(sliderGO.transform, false);
            RectTransform trackRect = trackGO.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 4f);
            trackRect.sizeDelta = new Vector2(0f, height - 32f);

            // Background（Slider 底图）
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(trackGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);

            // Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(trackGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.35f, 0.55f, 1f);

            // Handle Slide Area
            GameObject handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(trackGO.transform, false);
            RectTransform handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(4f, 0f);
            handleAreaRect.offsetMax = new Vector2(-4f, 0f);

            // Handle
            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            RectTransform handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(20f, height - 36f);
            Image handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            // 绑定 Slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            if (onValueChanged != null)
                slider.onValueChanged.AddListener(onValueChanged);

            return slider;
        }

        /// <summary>
        /// 获取默认字体。优先从 Resources/Fonts/Roboto-Regular 加载，
        /// 失败时回退到项目中已有的任意字体资产。
        /// </summary>
        public static Font GetDefaultFont()
        {
            // 1. 加载随项目一同放置的默认字体
            Font font = Resources.Load<Font>("Fonts/Roboto-Regular");
            if (font != null) return font;

            // 2. 兜底：搜索场景中已有的任意字体资产
#if UNITY_2023_1_OR_NEWER
            var fonts = UnityEngine.Object.FindObjectsByType<Font>(FindObjectsSortMode.None);
#else
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
#endif
            foreach (var f in fonts)
            {
                if (f != null) return f;
            }

            // 3. 最终兜底：从操作系统加载 Arial 动态字体
            try
            {
                Font systemFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
                if (systemFont != null) return systemFont;
            }
            catch { }

            Debug.LogError("[MenuUIHelper] 未能找到任何可用字体。请将字体文件放入 Assets/Resources/Fonts/ 目录，或在各 UI 脚本的 Inspector 中设置 Override Font。");
            return null;
        }
    }
}
