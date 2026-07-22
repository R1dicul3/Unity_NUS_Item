using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class DialogueController : MonoBehaviour {
    public enum SpeakerSide {
        Left,
        Right
    }

    [System.Serializable]
    public struct DialogueLine {
        public string speaker;
        [TextArea(2, 4)] public string text;
        public SpeakerSide speakerSide;
    }

    [System.Serializable]
    private class PortraitSlot {
        public Image image;
        public Text label;
        public TMP_Text tmpLabel;
        public Sprite sprite;
        public string fallbackName = "Character";
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private bool closeOnLastLine = true;

    [Header("Portraits")]
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Sprite leftPortraitSprite;
    [SerializeField] private Sprite rightPortraitSprite;
    [SerializeField] private PortraitSlot leftPortrait = new PortraitSlot { fallbackName = "Left" };
    [SerializeField] private PortraitSlot rightPortrait = new PortraitSlot { fallbackName = "Right" };
    [SerializeField] private Color activePortraitColor = Color.white;
    [SerializeField] private Color inactivePortraitColor = new Color(0.32f, 0.32f, 0.36f, 0.78f);

    [Header("Hierarchy UI")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text portraitLabel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private TMP_Text speakerTmpText;
    [SerializeField] private TMP_Text bodyTmpText;
    [SerializeField] private bool createFallbackUiIfMissing = true;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditMode = true;

    [Header("Portrait Layout")]
    [SerializeField] private Vector2 leftPortraitAnchorMin = new Vector2(0f, 0.34f);
    [SerializeField] private Vector2 leftPortraitAnchorMax = new Vector2(0.28f, 1f);
    [SerializeField] private Vector2 rightPortraitAnchorMin = new Vector2(0.72f, 0.34f);
    [SerializeField] private Vector2 rightPortraitAnchorMax = new Vector2(1f, 1f);
    [SerializeField] private Vector2 portraitOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 portraitOffsetMax = Vector2.zero;
    [SerializeField] private bool preservePortraitAspect = true;

    [Header("Dialogue Box Layout")]
    [SerializeField] private Vector2 dialogueBoxAnchorMin = new Vector2(0.04f, 0.04f);
    [SerializeField] private Vector2 dialogueBoxAnchorMax = new Vector2(0.96f, 0.38f);
    [SerializeField] private Vector2 dialogueBoxOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 dialogueBoxOffsetMax = Vector2.zero;

    [Header("Speaker Text Layout")]
    [SerializeField] private Vector2 speakerAnchorMin = new Vector2(0.04f, 0.72f);
    [SerializeField] private Vector2 speakerAnchorMax = new Vector2(0.96f, 0.93f);
    [SerializeField] private Vector2 speakerOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 speakerOffsetMax = Vector2.zero;
    [SerializeField] private int speakerFontSize = 40;

    [Header("Dialogue Text Layout")]
    [SerializeField] private Vector2 bodyAnchorMin = new Vector2(0.04f, 0.14f);
    [SerializeField] private Vector2 bodyAnchorMax = new Vector2(0.96f, 0.72f);
    [SerializeField] private Vector2 bodyOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 bodyOffsetMax = Vector2.zero;
    [SerializeField] private int bodyFontSize = 34;

    [Header("Portrait Label Layout")]
    [SerializeField] private int portraitLabelFontSize = 36;

    [Header("Placeholder Colors")]
    [SerializeField] private Color portraitColor = new Color(0.12f, 0.18f, 0.24f, 0.88f);
    [SerializeField] private Color dialogueBoxColor = new Color(0.03f, 0.04f, 0.06f, 0.9f);
    [SerializeField] private Color textColor = new Color(0.94f, 0.96f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent dialogueStarted;
    [SerializeField] private UnityEvent dialogueAdvanced;
    [SerializeField] private UnityEvent dialogueEnded;

    private int currentLineIndex;
    private bool isShowing;
    private bool uiSuppressed;
    private PlayerInputActions inputActions;

    public bool IsShowing => isShowing;
    public int CurrentLineIndex => currentLineIndex;

    public static DialogueController GetOrCreate() {
        DialogueController existing = FindFirstObjectByType<DialogueController>(FindObjectsInactive.Include);
        if (existing != null) {
            return existing;
        }

        GameObject dialogueObject = new GameObject("Dialogue Module");
        return dialogueObject.AddComponent<DialogueController>();
    }

    public static DialogueController Show(DialogueLine[] lines) {
        DialogueController controller = GetOrCreate();
        controller.StartDialogue(lines);
        return controller;
    }

    public static DialogueController Show(params string[] lines) {
        DialogueController controller = GetOrCreate();
        controller.StartDialogue(lines);
        return controller;
    }

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
            if (inputActions == null) {
                inputActions = new PlayerInputActions();
            }
            inputActions.Enable();
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

    private void OnDestroy() {
        if (Application.isPlaying) {
            inputActions?.Dispose();
            inputActions = null;
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
        if (!Application.isPlaying || !isShowing || uiSuppressed) {
            return;
        }

        if (ShouldAdvanceDialogue()) {
            Advance();
        }
    }

    private bool ShouldAdvanceDialogue() {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return (inputActions != null && inputActions.Player.NextDialogue.WasPressedThisFrame())
            || (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    public void SetLines(DialogueLine[] lines) {
        dialogueLines = lines;
    }

    public void SetPortrait(Sprite sprite) {
        SetPortrait(SpeakerSide.Left, sprite);
    }

    public void SetPortraits(Sprite leftSprite, Sprite rightSprite) {
        leftPortraitSprite = leftSprite;
        portraitSprite = leftSprite;
        rightPortraitSprite = rightSprite;
        ApplyPortraits(GetCurrentSpeakerSide());
    }

    public void SetPortrait(SpeakerSide side, Sprite sprite) {
        if (side == SpeakerSide.Left) {
            leftPortraitSprite = sprite;
            portraitSprite = sprite;
        }
        else {
            rightPortraitSprite = sprite;
        }

        ApplyPortraits(GetCurrentSpeakerSide());
    }

    public void SetPortraitName(SpeakerSide side, string displayName) {
        PortraitSlot slot = side == SpeakerSide.Left ? leftPortrait : rightPortrait;
        if (slot == null) {
            return;
        }

        slot.fallbackName = string.IsNullOrWhiteSpace(displayName) ? "Character" : displayName;
        ApplyPortraits(GetCurrentSpeakerSide());
    }

    public void StartDialogue(DialogueLine[] lines) {
        dialogueLines = lines;
        currentLineIndex = 0;
        isShowing = dialogueLines != null && dialogueLines.Length > 0;

        SetCanvasVisible(isShowing);

        if (isShowing) {
            ShowCurrentLine();
            dialogueStarted?.Invoke();
        }
        else {
            ApplyPortraits(SpeakerSide.Left);
        }
    }

    public void StartDialogue(params string[] lines) {
        if (lines == null) {
            StartDialogue((DialogueLine[])null);
            return;
        }

        DialogueLine[] convertedLines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) {
            convertedLines[i] = new DialogueLine {
                speaker = i % 2 == 0 ? GetPortraitName(SpeakerSide.Left) : GetPortraitName(SpeakerSide.Right),
                text = lines[i],
                speakerSide = i % 2 == 0 ? SpeakerSide.Left : SpeakerSide.Right
            };
        }

        StartDialogue(convertedLines);
    }

    public void StartDialogue(SpeakerSide firstSpeakerSide, params string[] lines) {
        if (lines == null) {
            StartDialogue((DialogueLine[])null);
            return;
        }

        DialogueLine[] convertedLines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) {
            SpeakerSide side = i % 2 == 0 ? firstSpeakerSide : Opposite(firstSpeakerSide);
            convertedLines[i] = new DialogueLine {
                speaker = GetPortraitName(side),
                text = lines[i],
                speakerSide = side
            };
        }

        StartDialogue(convertedLines);
    }

    public void Advance() {
        if (!isShowing) {
            return;
        }

        currentLineIndex++;
        dialogueAdvanced?.Invoke();

        if (currentLineIndex >= dialogueLines.Length) {
            if (closeOnLastLine) {
                HideDialogue();
            }
            else {
                currentLineIndex = dialogueLines.Length - 1;
            }
            return;
        }

        ShowCurrentLine();
    }

    public void HideDialogue() {
        bool wasShowing = isShowing;
        isShowing = false;
        SetCanvasVisible(false);

        if (wasShowing) {
            dialogueEnded?.Invoke();
        }
    }

    public void SetUiSuppressed(bool value) {
        uiSuppressed = value;
        SetCanvasVisible(isShowing);
    }

    private void ShowCurrentLine() {
        if (!HasRequiredUi() || dialogueLines == null || dialogueLines.Length == 0) {
            return;
        }

        currentLineIndex = Mathf.Clamp(currentLineIndex, 0, dialogueLines.Length - 1);
        DialogueLine line = dialogueLines[currentLineIndex];
        SpeakerSide speakerSide = line.speakerSide;
        string speaker = string.IsNullOrWhiteSpace(line.speaker) ? GetPortraitName(speakerSide) : line.speaker;

        SetText(speakerText, speakerTmpText, speaker);
        SetText(bodyText, bodyTmpText, line.text);
        ApplyPortraits(speakerSide);
    }

    private void ResolveHierarchyReferences() {
        EnsurePortraitSlotInstances();

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
            else if (leftPortrait.label == null && text.name == "LeftPortraitLabel") {
                leftPortrait.label = text;
            }
            else if (rightPortrait.label == null && text.name == "RightPortraitLabel") {
                rightPortrait.label = text;
            }
        }

        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in tmpTexts) {
            if (speakerTmpText == null && text.name == "SpeakerText") {
                speakerTmpText = text;
            }
            else if (bodyTmpText == null && text.name == "DialogueText") {
                bodyTmpText = text;
            }
            else if (leftPortrait.tmpLabel == null && text.name == "LeftPortraitLabel") {
                leftPortrait.tmpLabel = text;
            }
            else if (rightPortrait.tmpLabel == null && text.name == "RightPortraitLabel") {
                rightPortrait.tmpLabel = text;
            }
            else if (portraitLabel == null && leftPortrait.tmpLabel == null && text.name == "PortraitLabel") {
                leftPortrait.tmpLabel = text;
            }
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images) {
            if (portraitImage == null && image.name == "PortraitImage") {
                portraitImage = image;
            }
            else if (leftPortrait.image == null && (image.name == "LeftPortraitImage" || image.name == "PortraitImage")) {
                leftPortrait.image = image;
            }
            else if (rightPortrait.image == null && image.name == "RightPortraitImage") {
                rightPortrait.image = image;
            }
        }

        if (leftPortrait.image == null && portraitImage != null) {
            leftPortrait.image = portraitImage;
        }

        if (leftPortrait.label == null && portraitLabel != null) {
            leftPortrait.label = portraitLabel;
        }
    }

    private void EnsurePortraitSlotInstances() {
        if (leftPortrait == null) {
            leftPortrait = new PortraitSlot { fallbackName = "Left" };
        }

        if (rightPortrait == null) {
            rightPortrait = new PortraitSlot { fallbackName = "Right" };
        }
    }

    private bool HasRequiredUi() {
        return dialogueCanvas != null && HasSpeakerText() && HasBodyText();
    }

    private bool HasSpeakerText() {
        return speakerText != null || speakerTmpText != null;
    }

    private bool HasBodyText() {
        return bodyText != null || bodyTmpText != null;
    }

    private void InitializeUiIfNeeded() {
        ResolveHierarchyReferences();

        if (!HasRequiredUi() && createFallbackUiIfMissing) {
            BuildFallbackUi();
        }
        else if (dialogueCanvas != null && createFallbackUiIfMissing) {
            EnsurePortraitSlots();
        }

        SyncLegacyPortraitFields();
        ApplyLayout();
        ApplyPortraits(GetCurrentSpeakerSide());
    }

    private void EnsurePortraitSlots() {
        if (leftPortrait.image == null) {
            RectTransform left = CreatePanel("LeftPortraitImage", dialogueCanvas.transform, leftPortraitAnchorMin, leftPortraitAnchorMax, portraitColor);
            left.SetAsFirstSibling();
            leftPortrait.image = left.GetComponent<Image>();
            leftPortrait.label = CreateText("LeftPortraitLabel", left, leftPortrait.fallbackName, portraitLabelFontSize, TextAnchor.MiddleCenter);
        }

        if (rightPortrait.image == null) {
            RectTransform right = CreatePanel("RightPortraitImage", dialogueCanvas.transform, rightPortraitAnchorMin, rightPortraitAnchorMax, portraitColor);
            right.SetAsFirstSibling();
            rightPortrait.image = right.GetComponent<Image>();
            rightPortrait.label = CreateText("RightPortraitLabel", right, rightPortrait.fallbackName, portraitLabelFontSize, TextAnchor.MiddleCenter);
        }
    }

    private void SyncLegacyPortraitFields() {
        EnsurePortraitSlotInstances();

        if (leftPortrait.image == null && portraitImage != null) {
            leftPortrait.image = portraitImage;
        }

        if (portraitImage == null && leftPortrait.image != null) {
            portraitImage = leftPortrait.image;
        }

        if (leftPortrait.label == null && portraitLabel != null) {
            leftPortrait.label = portraitLabel;
        }

        if (portraitLabel == null && leftPortrait.label != null) {
            portraitLabel = leftPortrait.label;
        }

        if (leftPortraitSprite == null && portraitSprite != null) {
            leftPortraitSprite = portraitSprite;
        }

        leftPortrait.sprite = leftPortraitSprite;
        rightPortrait.sprite = rightPortraitSprite;
    }

    private void RefreshEditorPreview() {
        if (dialogueCanvas == null) {
            return;
        }

        dialogueCanvas.gameObject.SetActive(previewInEditMode);

        if (!previewInEditMode || !HasRequiredUi()) {
            return;
        }

        if (dialogueLines != null && dialogueLines.Length > 0) {
            currentLineIndex = Mathf.Clamp(currentLineIndex, 0, dialogueLines.Length - 1);
            ShowCurrentLine();
        }
        else {
            SetText(speakerText, speakerTmpText, GetPortraitName(SpeakerSide.Left));
            SetText(bodyText, bodyTmpText, "Dialogue preview text");
            ApplyPortraits(SpeakerSide.Left);
        }

        ApplyLayout();
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

        RectTransform left = CreatePanel("LeftPortraitImage", dialogueCanvas.transform, leftPortraitAnchorMin, leftPortraitAnchorMax, portraitColor);
        leftPortrait.image = left.GetComponent<Image>();
        leftPortrait.label = CreateText("LeftPortraitLabel", left, leftPortrait.fallbackName, portraitLabelFontSize, TextAnchor.MiddleCenter);

        RectTransform right = CreatePanel("RightPortraitImage", dialogueCanvas.transform, rightPortraitAnchorMin, rightPortraitAnchorMax, portraitColor);
        rightPortrait.image = right.GetComponent<Image>();
        rightPortrait.label = CreateText("RightPortraitLabel", right, rightPortrait.fallbackName, portraitLabelFontSize, TextAnchor.MiddleCenter);

        RectTransform dialogueBox = CreatePanel("DialogueBox", dialogueCanvas.transform, dialogueBoxAnchorMin, dialogueBoxAnchorMax, dialogueBoxColor);
        speakerText = CreateText("SpeakerText", dialogueBox, GetPortraitName(SpeakerSide.Left), speakerFontSize, TextAnchor.UpperLeft);
        bodyText = CreateText("DialogueText", dialogueBox, string.Empty, bodyFontSize, TextAnchor.UpperLeft);

        SyncLegacyPortraitFields();
        ApplyLayout();
    }

    private void ApplyLayout() {
        if (leftPortrait.image != null) {
            ApplyRect(leftPortrait.image.rectTransform, leftPortraitAnchorMin, leftPortraitAnchorMax, portraitOffsetMin, portraitOffsetMax);
            leftPortrait.image.preserveAspect = preservePortraitAspect;
        }

        if (rightPortrait.image != null) {
            ApplyRect(rightPortrait.image.rectTransform, rightPortraitAnchorMin, rightPortraitAnchorMax, portraitOffsetMin, portraitOffsetMax);
            rightPortrait.image.preserveAspect = preservePortraitAspect;
        }

        ApplyTextLayout(speakerText, speakerTmpText, speakerAnchorMin, speakerAnchorMax, speakerOffsetMin, speakerOffsetMax, speakerFontSize);
        ApplyTextLayout(bodyText, bodyTmpText, bodyAnchorMin, bodyAnchorMax, bodyOffsetMin, bodyOffsetMax, bodyFontSize);
        SetPortraitLabelFontSize(leftPortrait);
        SetPortraitLabelFontSize(rightPortrait);

        Transform dialogueBox = dialogueCanvas != null ? dialogueCanvas.transform.Find("DialogueBox") : null;
        if (dialogueBox is RectTransform dialogueBoxRect) {
            ApplyRect(dialogueBoxRect, dialogueBoxAnchorMin, dialogueBoxAnchorMax, dialogueBoxOffsetMin, dialogueBoxOffsetMax);
        }
    }

    private void ApplyPortraits(SpeakerSide activeSide) {
        SyncLegacyPortraitFields();
        ApplyPortrait(leftPortrait, activeSide == SpeakerSide.Left);
        ApplyPortrait(rightPortrait, activeSide == SpeakerSide.Right);
    }

    private void ApplyPortrait(PortraitSlot slot, bool isActiveSpeaker) {
        if (slot == null || slot.image == null) {
            return;
        }

        bool hasPortrait = slot.sprite != null;
        slot.image.sprite = slot.sprite;
        slot.image.color = hasPortrait
            ? (isActiveSpeaker ? activePortraitColor : inactivePortraitColor)
            : (isActiveSpeaker ? portraitColor : inactivePortraitColor);
        slot.image.type = Image.Type.Simple;
        slot.image.preserveAspect = preservePortraitAspect;

        bool showLabel = !hasPortrait;
        SetText(slot.label, slot.tmpLabel, slot.fallbackName);
        if (slot.label != null) {
            slot.label.gameObject.SetActive(showLabel);
        }
        if (slot.tmpLabel != null) {
            slot.tmpLabel.gameObject.SetActive(showLabel);
        }
    }

    private void SetCanvasVisible(bool visible) {
        if (dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(visible && !uiSuppressed);
        }
    }

    private SpeakerSide GetCurrentSpeakerSide() {
        if (dialogueLines == null || dialogueLines.Length == 0) {
            return SpeakerSide.Left;
        }

        int index = Mathf.Clamp(currentLineIndex, 0, dialogueLines.Length - 1);
        return dialogueLines[index].speakerSide;
    }

    private string GetPortraitName(SpeakerSide side) {
        PortraitSlot slot = side == SpeakerSide.Left ? leftPortrait : rightPortrait;
        return slot != null && !string.IsNullOrWhiteSpace(slot.fallbackName) ? slot.fallbackName : "Character";
    }

    private static SpeakerSide Opposite(SpeakerSide side) {
        return side == SpeakerSide.Left ? SpeakerSide.Right : SpeakerSide.Left;
    }

    private static void ApplyTextLayout(Text text, TMP_Text tmpText, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize) {
        if (text != null) {
            ApplyRect(text.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            text.fontSize = fontSize;
        }

        if (tmpText != null) {
            ApplyRect(tmpText.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            tmpText.fontSize = fontSize;
        }
    }

    private void SetPortraitLabelFontSize(PortraitSlot slot) {
        if (slot == null) {
            return;
        }

        if (slot.label != null) {
            slot.label.fontSize = portraitLabelFontSize;
        }

        if (slot.tmpLabel != null) {
            slot.tmpLabel.fontSize = portraitLabelFontSize;
        }
    }

    private static void SetText(Text text, TMP_Text tmpText, string value) {
        if (text != null) {
            text.text = value;
        }

        if (tmpText != null) {
            tmpText.text = value;
        }
    }

    private static void ApplyRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
