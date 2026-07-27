using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class AddLanguageButtonsToSettingsPrefab
{
    [MenuItem("Tools/Add Language Buttons to Settings Prefab")]
    public static void Execute()
    {
        string path = "Assets/Resources/UI/SettingsPanelCanvas.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("[AddLanguageButtons] Prefab not found at " + path);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        Transform content = FindChildRecursive(instance.transform, "Content");
        if (content == null)
        {
            Debug.LogError("[AddLanguageButtons] Content not found in prefab.");
            Object.DestroyImmediate(instance);
            return;
        }

        Transform backButton = FindChildRecursive(instance.transform, "BackButton");
        if (backButton == null)
        {
            Debug.LogError("[AddLanguageButtons] BackButton not found in prefab.");
            Object.DestroyImmediate(instance);
            return;
        }

        // LanguageSelector
        GameObject selector = new GameObject("LanguageSelector", typeof(RectTransform), typeof(LayoutElement));
        selector.transform.SetParent(content, false);
        RectTransform selectorRect = selector.GetComponent<RectTransform>();
        selectorRect.sizeDelta = new Vector2(560f, 55f);

        LayoutElement selectorLayout = selector.GetComponent<LayoutElement>();
        selectorLayout.preferredWidth = 560f;
        selectorLayout.preferredHeight = 55f;
        selectorLayout.flexibleHeight = 0f;

        // LanguageLabel
        GameObject labelGO = new GameObject("LanguageLabel", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(selector.transform, false);
        Text labelText = labelGO.GetComponent<Text>();
        labelText.text = "Language";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 28;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(0.35f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;

        // ButtonContainer
        GameObject btnContainer = new GameObject("ButtonContainer", typeof(RectTransform));
        btnContainer.transform.SetParent(selector.transform, false);
        RectTransform btnContainerRect = btnContainer.GetComponent<RectTransform>();
        btnContainerRect.anchorMin = new Vector2(0.38f, 0f);
        btnContainerRect.anchorMax = Vector2.one;
        btnContainerRect.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRect.anchoredPosition = Vector2.zero;
        btnContainerRect.sizeDelta = Vector2.zero;

        // English Button
        CreateLangButton(btnContainer.transform, "LanguageEnglishButton", "English",
            new Vector2(0f, 0.5f), new Vector2(0.48f, 0.5f));

        // Chinese Button
        CreateLangButton(btnContainer.transform, "LanguageChineseButton", "中文",
            new Vector2(0.52f, 0.5f), new Vector2(1f, 0.5f));

        // Move selector before BackButton
        selector.transform.SetSiblingIndex(backButton.GetSiblingIndex());

        // Save
        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
        Object.DestroyImmediate(instance);

        Debug.Log("[AddLanguageButtons] Language buttons added to SettingsPanelCanvas.prefab successfully!");
    }

    static void CreateLangButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        Image img = btnGO.GetComponent<Image>();
        img.color = new Color(0.25f, 0.35f, 0.55f, 1f);

        Button btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;

        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(0f, 45f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(btnGO.transform, false);
        Text txt = textGO.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
