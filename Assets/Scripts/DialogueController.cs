using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool showOnStart = true;

    [Header("Placeholder Colors")]
    [SerializeField] private Color portraitColor = new Color(0.12f, 0.18f, 0.24f, 0.88f);
    [SerializeField] private Color dialogueBoxColor = new Color(0.03f, 0.04f, 0.06f, 0.88f);
    [SerializeField] private Color textColor = new Color(0.94f, 0.96f, 1f);

    private Canvas canvas;
    private Text speakerText;
    private Text bodyText;
    private int currentLineIndex;
    private bool isShowing;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        BuildUi();
    }

    private void Start()
    {
        if (showOnStart && dialogueLines != null && dialogueLines.Length > 0)
        {
            StartDialogue(dialogueLines);
        }
        else
        {
            HideDialogue();
        }
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool confirmed = keyboard != null && keyboard.enterKey.wasPressedThisFrame;

        if (clicked || confirmed)
        {
            Advance();
        }
    }

    public void SetLines(DialogueLine[] lines)
    {
        dialogueLines = lines;
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        dialogueLines = lines;
        currentLineIndex = 0;
        isShowing = dialogueLines != null && dialogueLines.Length > 0;
        canvas.gameObject.SetActive(isShowing);

        if (isShowing)
        {
            ShowCurrentLine();
        }
    }

    public void StartDialogue(params string[] lines)
    {
        DialogueLine[] convertedLines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            convertedLines[i] = new DialogueLine { speaker = "Character", text = lines[i] };
        }

        StartDialogue(convertedLines);
    }

    public void Advance()
    {
        if (!isShowing)
        {
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= dialogueLines.Length)
        {
            HideDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void HideDialogue()
    {
        isShowing = false;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = dialogueLines[currentLineIndex];
        speakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? "Character" : line.speaker;
        bodyText.text = line.text;
    }

    private void BuildUi()
    {
        canvas = new GameObject("Dialogue Canvas").AddComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform portrait = CreatePanel("Portrait Placeholder", canvas.transform, new Vector2(0f, 0.38f), new Vector2(0.5f, 1f), portraitColor);
        Text portraitLabel = CreateText("Portrait Label", portrait, "Portrait Placeholder", 42, TextAnchor.MiddleCenter);
        portraitLabel.color = new Color(0.72f, 0.82f, 0.92f, 0.9f);

        RectTransform dialogueBox = CreatePanel("Dialogue Box Placeholder", canvas.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.4f), dialogueBoxColor);
        speakerText = CreateText("Speaker Text", dialogueBox, "Character", 40, TextAnchor.UpperLeft);
        bodyText = CreateText("Dialogue Text", dialogueBox, "", 34, TextAnchor.UpperLeft);

        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0.04f, 0.72f);
        speakerRect.anchorMax = new Vector2(0.96f, 0.93f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.04f, 0.16f);
        bodyRect.anchorMax = new Vector2(0.96f, 0.72f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
    }

    private RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 20f);
        rect.offsetMax = new Vector2(-24f, -20f);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.color = textColor;
        return text;
    }
}
