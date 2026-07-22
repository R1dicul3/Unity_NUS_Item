using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace MainMenu.Editor
{
    /// <summary>
    /// 在 Unity 编辑器中一键生成 SettingsPanelCanvas.prefab。
    /// 菜单路径：Tools / UI / Build Settings Panel Prefab
    /// </summary>
    public static class SettingsPrefabBuilder
    {
        [MenuItem("Tools/UI/Build Settings Panel Prefab")]
        public static void BuildPrefab()
        {
            string prefabPath = "Assets/Resources/UI/SettingsPanelCanvas.prefab";

            // 创建 Canvas 根
            GameObject canvasGO = new GameObject("SettingsPanelCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // 全屏背景
            GameObject bgGO = new GameObject("DimBackground");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // 设置面板
            GameObject panelGO = new GameObject("SettingsPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(640f, 560f);

            // Content 容器
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(40f, 40f);
            contentRect.offsetMax = new Vector2(-40f, -40f);

            VerticalLayoutGroup contentVlg = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment = TextAnchor.MiddleCenter;
            contentVlg.spacing = 12f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            ContentSizeFitter contentCsf = contentGO.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Font font = MenuUIHelper.GetDefaultFont();

            // 标题
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(contentGO.transform, false);
            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = "Settings";
            titleText.font = font;
            titleText.fontSize = 56;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0f, 90f);

            // Spacer
            GameObject spacer1 = new GameObject("Spacer");
            spacer1.transform.SetParent(contentGO.transform, false);
            spacer1.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 16f);

            // Master Volume Slider
            CreateSliderInContent(contentGO.transform, "MasterVolumeSlider", "Master Volume", font);

            // BGM Volume Slider
            CreateSliderInContent(contentGO.transform, "BGMVolumeSlider", "BGM Volume", font);

            // SFX Volume Slider
            CreateSliderInContent(contentGO.transform, "SFXVolumeSlider", "SFX Volume", font);

            // Spacer
            GameObject spacer2 = new GameObject("Spacer");
            spacer2.transform.SetParent(contentGO.transform, false);
            spacer2.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

            // Back Button
            GameObject backGO = new GameObject("BackButton");
            backGO.transform.SetParent(contentGO.transform, false);
            Button backBtn = backGO.AddComponent<Button>();
            Image backImg = backGO.AddComponent<Image>();
            backImg.color = new Color(0.25f, 0.35f, 0.55f, 1f);
            backBtn.targetGraphic = backImg;
            RectTransform backRect = backGO.GetComponent<RectTransform>();
            backRect.sizeDelta = new Vector2(200f, 55f);

            GameObject backTextGO = new GameObject("Text");
            backTextGO.transform.SetParent(backGO.transform, false);
            Text backText = backTextGO.AddComponent<Text>();
            backText.text = "< Back";
            backText.font = font;
            backText.fontSize = 24;
            backText.color = Color.white;
            backText.alignment = TextAnchor.MiddleCenter;
            RectTransform backTextRect = backTextGO.GetComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;

            // 确保目录存在
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // 保存 Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
            Object.DestroyImmediate(canvasGO);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Settings Prefab", $"Prefab saved to {prefabPath}", "OK");
        }

        static void CreateSliderInContent(Transform content, string name, string label, Font font)
        {
            float width = 560f;
            float height = 70f;

            GameObject rowGO = new GameObject(name + " Row");
            rowGO.transform.SetParent(content, false);
            RectTransform rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(width, height);

            LayoutElement rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = width;
            rowLayout.preferredHeight = height;

            VerticalLayoutGroup rowVlg = rowGO.AddComponent<VerticalLayoutGroup>();
            rowVlg.childAlignment = TextAnchor.MiddleCenter;
            rowVlg.spacing = 4f;
            rowVlg.childControlWidth = true;
            rowVlg.childControlHeight = false;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;

            // Label Row
            GameObject labelRowGO = new GameObject("LabelRow");
            labelRowGO.transform.SetParent(rowGO.transform, false);
            RectTransform labelRowRect = labelRowGO.AddComponent<RectTransform>();
            labelRowRect.sizeDelta = new Vector2(width, 28f);

            LayoutElement labelRowLayout = labelRowGO.AddComponent<LayoutElement>();
            labelRowLayout.preferredWidth = width;
            labelRowLayout.preferredHeight = 28f;
            labelRowLayout.flexibleHeight = 0f;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(labelRowGO.transform, false);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = font;
            labelText.fontSize = 28;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-60f, 0f);

            // Value
            GameObject valueGO = new GameObject("ValueText");
            valueGO.transform.SetParent(labelRowGO.transform, false);
            Text valText = valueGO.AddComponent<Text>();
            valText.text = "100%";
            valText.font = font;
            valText.fontSize = 24;
            valText.color = Color.white;
            valText.alignment = TextAnchor.MiddleRight;
            RectTransform valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(60f, 0f);

            // Slider
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(rowGO.transform, false);
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(width, height - 32f);

            LayoutElement sliderLayout = sliderGO.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = width;
            sliderLayout.preferredHeight = height - 32f;

            // Track
            GameObject trackGO = new GameObject("Track");
            trackGO.transform.SetParent(sliderGO.transform, false);
            RectTransform trackRect = trackGO.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 4f);
            trackRect.sizeDelta = new Vector2(0f, height - 36f);

            // Background
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
            handleRect.sizeDelta = new Vector2(20f, height - 40f);
            Image handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
        }
    }
}
