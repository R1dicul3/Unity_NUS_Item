#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor 工具：一键生成 EndingCanvas Prefab。
/// 生成后可自由在 Inspector 中修改文字、分页和显示模式。
/// </summary>
public static class CreateEndingPrefab
{
    [MenuItem("Tools/UI/Create Ending Prefab")]
    public static void Generate()
    {
        // 1. Canvas
        GameObject canvasGO = new GameObject("EndingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.sizeDelta = Vector2.zero;

        // 2. Background (RawImage)
        GameObject bgGO = new GameObject("Background", typeof(RawImage));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RawImage bgImage = bgGO.GetComponent<RawImage>();
        bgImage.color = new Color(0.08f, 0.12f, 0.17f, 0.95f);
        bgImage.raycastTarget = true;

        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 3. PagesContainer
        GameObject pagesContainerGO = new GameObject("PagesContainer");
        pagesContainerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform pagesRect = pagesContainerGO.AddComponent<RectTransform>();
        pagesRect.anchorMin = Vector2.zero;
        pagesRect.anchorMax = Vector2.one;
        pagesRect.offsetMin = Vector2.zero;
        pagesRect.offsetMax = Vector2.zero;

        // 4. Create sample pages
        GameObject page1 = CreatePage(pagesContainerGO.transform, "Page 1");
        CreateLine(page1.transform, "Thank you for playing.");
        CreateLine(page1.transform, "Your journey has come to an end.");

        GameObject page2 = CreatePage(pagesContainerGO.transform, "Page 2");
        CreateLine(page2.transform, "But every ending is a new beginning.");
        CreateLine(page2.transform, "We hope to see you again.");

        // 5. EndingCarousel
        MainMenu.EndingCarousel carousel = pagesContainerGO.AddComponent<MainMenu.EndingCarousel>();
        carousel.pages = new MainMenu.EndingPage[]
        {
            new MainMenu.EndingPage
            {
                content = page1,
                displayMode = MainMenu.DisplayMode.FadeAll,
                displayDuration = 3f
            },
            new MainMenu.EndingPage
            {
                content = page2,
                displayMode = MainMenu.DisplayMode.TypewriterLines,
                displayDuration = 0.5f
            }
        };

        // 6. SkipPrompt
        GameObject skipPromptGO = new GameObject("SkipPrompt", typeof(TextMeshProUGUI));
        skipPromptGO.transform.SetParent(canvasGO.transform, false);
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

        carousel.skipPrompt = skipPromptGO;

        // 7. Save prefab
        string prefabPath = "Assets/Resources/UI/EndingCanvas.prefab";
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Ending prefab created at:\n{prefabPath}", "OK");
    }

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
}
#endif
