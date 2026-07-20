using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class DialogueController : MonoBehaviour {
    [System.Serializable]
    public struct DialogueLine {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool showOnStart = true;

    [Header("Portrait")]
    [SerializeField] private Sprite portraitSprite;

    [Header("Hierarchy UI")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text portraitLabel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private bool createFallbackUiIfMissing;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditMode = true;

    [Header("Portrait Layout")]
    [SerializeField] private Vector2 portraitAnchorMin = new Vector2(0f, 0.38f);
    [SerializeField] private Vector2 portraitAnchorMax = new Vector2(0.5f, 1f);
    [SerializeField] private Vector2 portraitOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 portraitOffsetMax = Vector2.zero;
    [SerializeField] private bool preservePortraitAspect = true;

    [Header("Dialogue Box Layout")]
    [SerializeField] private Vector2 dialogueBoxAnchorMin = new Vector2(0.04f, 0.04f);
    [SerializeField] private Vector2 dialogueBoxAnchorMax = new Vector2(0.96f, 0.4f);
    [SerializeField] private Vector2 dialogueBoxOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 dialogueBoxOffsetMax = Vector2.zero;

    [Header("Speaker Text Layout")]
    [SerializeField] private Vector2 speakerAnchorMin = new Vector2(0.04f, 0.72f);
    [SerializeField] private Vector2 speakerAnchorMax = new Vector2(0.96f, 0.93f);
    [SerializeField] private Vector2 speakerOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 speakerOffsetMax = Vector2.zero;
    [SerializeField] private int speakerFontSize = 40;

    [Header("Dialogue Text Layout")]
    [SerializeField] private Vector2 bodyAnchorMin = new Vector2(0.04f, 0.16f);
    [SerializeField] private Vector2 bodyAnchorMax = new Vector2(0.96f, 0.72f);
    [SerializeField] private Vector2 bodyOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 bodyOffsetMax = Vector2.zero;
    [SerializeField] private int bodyFontSize = 34;

    [Header("Portrait Label Layout")]
    [SerializeField] private int portraitLabelFontSize = 42;

    [Header("Placeholder Colors")]
    [SerializeField] private Color portraitColor = new Color(0.12f, 0.18f, 0.24f, 0.88f);
    [SerializeField] private Color dialogueBoxColor = new Color(0.03f, 0.04f, 0.06f, 0.88f);
    [SerializeField] private Color textColor = new Color(0.94f, 0.96f, 1f);

    private int currentLineIndex;
    private bool isShowing;
    private PlayerInputActions inputActions;

    public bool IsShowing => isShowing;

    private void Awake() {
        if (Application.isPlaying) {
            inputActions = new PlayerInputActions();
        }

        InitializeUiIfNeeded();

        if (!Application.isPlaying) {
            RefreshEditorPreview();
        }
    }

    private void OnEnable() {
        if (Application.isPlaying) {
            inputActions?.Enable();
            return;
        }

        InitializeUiIfNeeded();
        RefreshEditorPreview();
    }

    private void OnDisable() {
        if (Application.isPlaying) {
            inputActions?.Disable();
        }
    }

    private void OnValidate() {
        InitializeUiIfNeeded();

        if (!Application.isPlaying) {
            RefreshEditorPreview();
        }
    }

    private void Start() {
        if (!Application.isPlaying) {
            return;
        }

        if (showOnStart && dialogueLines != null && dialogueLines.Length > 0) {
            StartDialogue(dialogueLines);
        }
        else {
            HideDialogue();
        }
    }

    private void Update() {
        if (!Application.isPlaying) {
            return;
        }

        if (!isShowing) {
            return;
        }

        if (inputActions.Player.NextDialogue.WasPressedThisFrame()) {
            Advance();
        }
    }

    public void SetLines(DialogueLine[] lines) {
        dialogueLines = lines;
    }

    public void SetPortrait(Sprite sprite) {
        portraitSprite = sprite;
        ApplyPortrait();
    }

    public void StartDialogue(DialogueLine[] lines) {
        dialogueLines = lines;
        currentLineIndex = 0;
        isShowing = dialogueLines != null && dialogueLines.Length > 0;

        if (dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(isShowing);
        }

        if (isShowing) {
            ShowCurrentLine();
        }
    }

    public void StartDialogue(params string[] lines) {
        DialogueLine[] convertedLines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) {
            convertedLines[i] = new DialogueLine { speaker = "Character", text = lines[i] };
        }

        StartDialogue(convertedLines);
    }

    public void Advance() {
        if (!isShowing) {
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= dialogueLines.Length) {
            HideDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void HideDialogue() {
        isShowing = false;
        if (dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(false);
        }
    }

    private void ShowCurrentLine() {
        if (speakerText == null || bodyText == null) {
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        speakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? "Character" : line.speaker;
        bodyText.text = line.text;
    }

    private void ResolveHierarchyReferences() {
        if (dialogueCanvas == null) {
            dialogueCanvas = GetComponentInChildren<Canvas>(true);
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts) {
            if (speakerText == null && text.name == "SpeakerText") {
                speakerText = text;
            }
            else if (bodyText == null && text.name == "DialogueText") {
                bodyText = text;
            }
            else if (portraitLabel == null && text.name == "PortraitLabel") {
                portraitLabel = text;
            }
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images) {
            if (portraitImage == null && image.name == "PortraitImage") {
                portraitImage = image;
            }
        }
    }

    private bool HasRequiredUi() {
        return dialogueCanvas != null && speakerText != null && bodyText != null;
    }

    private void InitializeUiIfNeeded() {
        ResolveHierarchyReferences();

        if (!HasRequiredUi() && createFallbackUiIfMissing) {
            BuildFallbackUi();
        }

        ApplyLayout();
        ApplyPortrait();
    }

    private void RefreshEditorPreview() {
        if (dialogueCanvas == null) {
            return;
        }

        dialogueCanvas.gameObject.SetActive(previewInEditMode);

        if (!previewInEditMode || speakerText == null || bodyText == null) {
            return;
        }

        if (dialogueLines != null && dialogueLines.Length > 0) {
            currentLineIndex = Mathf.Clamp(currentLineIndex, 0, dialogueLines.Length - 1);
            ShowCurrentLine();
        }
        else {
            speakerText.text = "Character";
            bodyText.text = "Dialogue preview text";
        }

        ApplyLayout();
        ApplyPortrait();
    }

    private void BuildFallbackUi() {
        dialogueCanvas = new GameObject("DialogueCanvas").AddComponent<Canvas>();
        dialogueCanvas.transform.SetParent(transform, false);
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 20;

        CanvasScaler scaler = dialogueCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        dialogueCanvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform portrait = CreatePanel("PortraitImage", dialogueCanvas.transform, portraitAnchorMin, portraitAnchorMax, portraitColor);
        portraitImage = portrait.GetComponent<Image>();
        portraitImage.preserveAspect = preservePortraitAspect;
        portraitLabel = CreateText("PortraitLabel", portrait, "Portrait Placeholder", portraitLabelFontSize, TextAnchor.MiddleCenter);
        portraitLabel.color = new Color(0.72f, 0.82f, 0.92f, 0.9f);
        ApplyPortrait();

        RectTransform dialogueBox = CreatePanel("DialogueBox", dialogueCanvas.transform, dialogueBoxAnchorMin, dialogueBoxAnchorMax, dialogueBoxColor);
        speakerText = CreateText("SpeakerText", dialogueBox, "Character", speakerFontSize, TextAnchor.UpperLeft);
        bodyText = CreateText("DialogueText", dialogueBox, "", bodyFontSize, TextAnchor.UpperLeft);
        ApplyLayout();
    }

    private void ApplyLayout() {
        if (portraitImage != null) {
            ApplyRect(portraitImage.rectTransform, portraitAnchorMin, portraitAnchorMax, portraitOffsetMin, portraitOffsetMax);
            portraitImage.preserveAspect = preservePortraitAspect;
        }

        if (speakerText != null) {
            ApplyRect(speakerText.rectTransform, speakerAnchorMin, speakerAnchorMax, speakerOffsetMin, speakerOffsetMax);
            speakerText.fontSize = speakerFontSize;
        }

        if (bodyText != null) {
            ApplyRect(bodyText.rectTransform, bodyAnchorMin, bodyAnchorMax, bodyOffsetMin, bodyOffsetMax);
            bodyText.fontSize = bodyFontSize;
        }

        if (portraitLabel != null) {
            portraitLabel.fontSize = portraitLabelFontSize;
        }

        Transform dialogueBox = dialogueCanvas != null ? dialogueCanvas.transform.Find("DialogueBox") : null;
        if (dialogueBox is RectTransform dialogueBoxRect) {
            ApplyRect(dialogueBoxRect, dialogueBoxAnchorMin, dialogueBoxAnchorMax, dialogueBoxOffsetMin, dialogueBoxOffsetMax);
        }
    }

    private static void ApplyRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void ApplyPortrait() {
        if (portraitImage == null) {
            return;
        }

        bool hasPortrait = portraitSprite != null;
        portraitImage.sprite = portraitSprite;
        portraitImage.color = hasPortrait ? Color.white : portraitColor;
        portraitImage.type = Image.Type.Simple;

        if (portraitLabel != null) {
            portraitLabel.gameObject.SetActive(!hasPortrait);
        }
    }

    private RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color) {
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

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment) {
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