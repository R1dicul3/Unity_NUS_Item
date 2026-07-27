#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class CreateCreditsPrefab
{
    [MenuItem("Tools/UI/Create Credits Prefab")]
    public static void Generate()
    {
        // 1. Canvas
        GameObject canvasGO = new GameObject("CreditsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        // 3. Content (centered vertical layout)
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(canvasGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(600f, 800f);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 20f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(20, 20, 20, 20);

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. TitleText
        GameObject titleGO = new GameObject("TitleText", typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(contentGO.transform, false);
        TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = "Credits";
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(600f, 80f);

        // 5. CreditsContent (dynamic entries container)
        GameObject creditsContentGO = new GameObject("CreditsContent");
        creditsContentGO.transform.SetParent(contentGO.transform, false);
        RectTransform creditsContentRect = creditsContentGO.AddComponent<RectTransform>();
        creditsContentRect.sizeDelta = new Vector2(600f, 400f);

        VerticalLayoutGroup creditsVlg = creditsContentGO.AddComponent<VerticalLayoutGroup>();
        creditsVlg.childAlignment = TextAnchor.UpperCenter;
        creditsVlg.spacing = 16f;
        creditsVlg.childControlWidth = true;
        creditsVlg.childControlHeight = false;
        creditsVlg.childForceExpandWidth = true;
        creditsVlg.childForceExpandHeight = false;

        ContentSizeFitter creditsFitter = creditsContentGO.AddComponent<ContentSizeFitter>();
        creditsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 6. BackButton
        GameObject backGO = new GameObject("BackButton", typeof(Image), typeof(Button));
        backGO.transform.SetParent(contentGO.transform, false);

        Image backImage = backGO.GetComponent<Image>();
        backImage.sprite = Resources.GetBuiltinResource<Sprite>("UISprite");
        backImage.type = Image.Type.Sliced;
        backImage.color = new Color(0.25f, 0.35f, 0.55f, 1f);

        Button backButton = backGO.GetComponent<Button>();
        backButton.targetGraphic = backImage;

        RectTransform backRect = backGO.GetComponent<RectTransform>();
        backRect.sizeDelta = new Vector2(260f, 55f);

        // Button text
        GameObject backTextGO = new GameObject("Text (TMP)", typeof(TextMeshProUGUI));
        backTextGO.transform.SetParent(backGO.transform, false);
        TextMeshProUGUI backText = backTextGO.GetComponent<TextMeshProUGUI>();
        backText.text = "< Back";
        backText.fontSize = 24;
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;
        backText.raycastTarget = false;

        RectTransform backTextRect = backTextGO.GetComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.offsetMin = Vector2.zero;
        backTextRect.offsetMax = Vector2.zero;

        // 7. Save prefab
        string prefabPath = "Assets/Resources/UI/CreditsCanvas.prefab";
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // Remove existing if any
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Credits prefab created at:\n{prefabPath}", "OK");
    }
}
#endif
