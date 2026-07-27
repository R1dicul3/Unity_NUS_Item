using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class DialogueController : MonoBehaviour {

    public enum SpeakerSide { None, Left, Right }

    [System.Serializable]
    public struct DialogueLine {
        public string speaker;
        [TextArea(2, 4)] public string text;

        [Header("Portraits")]
        public Sprite leftPortrait;
        public Sprite rightPortrait;
        [Tooltip("当前说话的一方。非说话方的立绘会自动变暗。")]
        public SpeakerSide activeSide;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool showOnStart = true;

    [Header("Hierarchy UI")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private CinematicLetterbox cinematicLetterbox;
    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private Text leftPortraitLabel;
    [SerializeField] private Image rightPortraitImage;
    [SerializeField] private Text rightPortraitLabel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private bool createFallbackUiIfMissing;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditMode = true;

    [Header("Portrait Settings")]
    [SerializeField] private bool preservePortraitAspect = true;
    [SerializeField] private int portraitLabelFontSize = 42;

    [Header("Dialogue Box Layout")]
    [SerializeField] private Vector2 dialogueBoxAnchorMin = new Vector2(0.04f, 0.04f);
    [SerializeField] private Vector2 dialogueBoxAnchorMax = new Vector2(0.96f, 0.4f);
    [SerializeField] private Vector2 dialogueBoxOffsetMin = Vector2.zero;
    [SerializeField] private Vector2 dialogueBoxOffsetMax = Vector2.zero;

    [Header("Cinematic Letterbox")]
    [SerializeField] private bool useCinematicLetterbox = true;

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

    [Header("Colors")]
    [SerializeField] private Color activePortraitColor = Color.white;
    [SerializeField] private Color inactivePortraitColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 未说话时变暗
    [SerializeField] private Color dialogueBoxColor = new Color(0.03f, 0.04f, 0.06f, 0.88f);
    [SerializeField] private Color textColor = new Color(0.94f, 0.96f, 1f);

    private int currentLineIndex;
    private bool isShowing;
    private bool uiSuppressed;
    private PlayerInputActions inputActions;

    // 【新增变量】记录对话框被打开的帧数，用于防止同一帧双重触发
    private int openedFrame = -1;

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

    private void OnDestroy() {
        if (Application.isPlaying) {
            inputActions?.Dispose();
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

        if (!isShowing || uiSuppressed) {
            return;
        }

        if (ShouldAdvanceDialogue()) {
            // 【核心修复】：只有当当前帧大于打开对话的那一帧时，才允许翻页
            if (Time.frameCount > openedFrame) {
                Advance();
            }
        }
    }

    private bool ShouldAdvanceDialogue() {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        return inputActions.Player.NextDialogue.WasPressedThisFrame()
            || (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    public void SetLines(DialogueLine[] lines) {
        dialogueLines = lines;
    }

    public void StartStoredDialogue() {
        StartDialogue(dialogueLines);
    }

    public void StartDialogue(DialogueLine[] lines) {
        dialogueLines = lines;
        currentLineIndex = 0;
        isShowing = dialogueLines != null && dialogueLines.Length > 0;

        // 【核心修复】：记录对话框在此帧被打开
        openedFrame = Time.frameCount;

        if (dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(isShowing && !uiSuppressed);
        }

        if (isShowing) {
            ShowLetterbox();
            ShowCurrentLine();
        }
    }

    public void StartDialogue(params string[] lines) {
        DialogueLine[] convertedLines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) {
            convertedLines[i] = new DialogueLine { speaker = "Character", text = lines[i], activeSide = SpeakerSide.Left };
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
            if (Application.isPlaying && !uiSuppressed && HasLetterbox()) {
                cinematicLetterbox.Hide(DeactivateCanvasIfDialogueStillHidden);
            }
            else {
                HideLetterboxImmediate();
                dialogueCanvas.gameObject.SetActive(false);
            }
        }
    }

    public void SetUiSuppressed(bool value) {
        uiSuppressed = value;
        if (dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(isShowing && !uiSuppressed);
        }

        if (!uiSuppressed && isShowing) {
            ShowLetterboxImmediate();
        }
    }

    private void ShowCurrentLine() {
        if (speakerText == null || bodyText == null) {
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        speakerText.text = string.IsNullOrWhiteSpace(line.speaker) ? "Character" : line.speaker;
        bodyText.text = line.text;

        UpdatePortraits(line);
    }

    private void UpdatePortraits(DialogueLine line) {
        ApplySinglePortrait(leftPortraitImage, leftPortraitLabel, line.leftPortrait, line.activeSide == SpeakerSide.Left || line.activeSide == SpeakerSide.None);
        ApplySinglePortrait(rightPortraitImage, rightPortraitLabel, line.rightPortrait, line.activeSide == SpeakerSide.Right || line.activeSide == SpeakerSide.None);
    }

    private void ApplySinglePortrait(Image img, Text label, Sprite sprite, bool isActiveSpeaker) {
        if (img == null) return;

        bool hasPortrait = sprite != null;
        img.sprite = sprite;

        if (hasPortrait) {
            img.color = isActiveSpeaker ? activePortraitColor : inactivePortraitColor;
        }
        else {
            img.color = Color.clear;
        }

        img.type = Image.Type.Simple;
        img.preserveAspect = preservePortraitAspect;

        if (label != null) {
            label.gameObject.SetActive(!hasPortrait);
            label.fontSize = portraitLabelFontSize;
        }
    }

    private void ResolveHierarchyReferences() {
        if (dialogueCanvas == null) {
            dialogueCanvas = GetComponentInChildren<Canvas>(true);
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts) {
            if (speakerText == null && text.name == "SpeakerText") speakerText = text;
            else if (bodyText == null && text.name == "DialogueText") bodyText = text;
            else if (leftPortraitLabel == null && text.name == "LeftPortraitLabel") leftPortraitLabel = text;
            else if (rightPortraitLabel == null && text.name == "RightPortraitLabel") rightPortraitLabel = text;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images) {
            if (leftPortraitImage == null && image.name == "LeftPortraitImage") leftPortraitImage = image;
            else if (rightPortraitImage == null && image.name == "RightPortraitImage") rightPortraitImage = image;
        }

        if (cinematicLetterbox == null && dialogueCanvas != null) {
            cinematicLetterbox = dialogueCanvas.GetComponentInChildren<CinematicLetterbox>(true);
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

        EnsureLetterbox();
        ApplyLayout();

        if (!Application.isPlaying && dialogueLines != null && dialogueLines.Length > 0) {
            UpdatePortraits(dialogueLines[Mathf.Clamp(currentLineIndex, 0, dialogueLines.Length - 1)]);
        }
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

            ApplySinglePortrait(leftPortraitImage, leftPortraitLabel, null, true);
            ApplySinglePortrait(rightPortraitImage, rightPortraitLabel, null, true);
        }

        ApplyLayout();

        if (previewInEditMode && useCinematicLetterbox) {
            ShowLetterboxImmediate();
        }
        else {
            HideLetterboxImmediate();
        }
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

        RectTransform leftPortrait = CreatePanel("LeftPortraitImage", dialogueCanvas.transform, new Vector2(0f, 0.38f), new Vector2(0.3f, 1f), Color.clear);
        leftPortraitImage = leftPortrait.GetComponent<Image>();
        leftPortraitImage.preserveAspect = preservePortraitAspect;
        leftPortraitLabel = CreateText("LeftPortraitLabel", leftPortrait, "Left Portrait", portraitLabelFontSize, TextAnchor.MiddleCenter);
        leftPortraitLabel.color = new Color(0.72f, 0.82f, 0.92f, 0.9f);

        RectTransform rightPortrait = CreatePanel("RightPortraitImage", dialogueCanvas.transform, new Vector2(0.7f, 0.38f), new Vector2(1f, 1f), Color.clear);
        rightPortraitImage = rightPortrait.GetComponent<Image>();
        rightPortraitImage.preserveAspect = preservePortraitAspect;
        rightPortraitLabel = CreateText("RightPortraitLabel", rightPortrait, "Right Portrait", portraitLabelFontSize, TextAnchor.MiddleCenter);
        rightPortraitLabel.color = new Color(0.72f, 0.82f, 0.92f, 0.9f);

        RectTransform dialogueBox = CreatePanel("DialogueBox", dialogueCanvas.transform, dialogueBoxAnchorMin, dialogueBoxAnchorMax, dialogueBoxColor);
        speakerText = CreateText("SpeakerText", dialogueBox, "Character", speakerFontSize, TextAnchor.UpperLeft);
        bodyText = CreateText("DialogueText", dialogueBox, "", bodyFontSize, TextAnchor.UpperLeft);

        EnsureLetterbox();
        ApplyLayout();
    }

    private void ApplyLayout() {
        if (speakerText != null) {
            ApplyRect(speakerText.rectTransform, speakerAnchorMin, speakerAnchorMax, speakerOffsetMin, speakerOffsetMax);
            speakerText.fontSize = speakerFontSize;
        }

        if (bodyText != null) {
            ApplyRect(bodyText.rectTransform, bodyAnchorMin, bodyAnchorMax, bodyOffsetMin, bodyOffsetMax);
            bodyText.fontSize = bodyFontSize;
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

    private void EnsureLetterbox() {
        if (!useCinematicLetterbox || dialogueCanvas == null) {
            return;
        }

        if (cinematicLetterbox == null) {
            Transform existing = dialogueCanvas.transform.Find("CinematicLetterbox");
            if (existing != null) {
                cinematicLetterbox = existing.GetComponent<CinematicLetterbox>();
            }
        }

        if (cinematicLetterbox == null) {
            GameObject letterboxObject = new GameObject("CinematicLetterbox");
            letterboxObject.transform.SetParent(dialogueCanvas.transform, false);
            RectTransform letterboxRect = letterboxObject.AddComponent<RectTransform>();
            letterboxRect.anchorMin = Vector2.zero;
            letterboxRect.anchorMax = Vector2.one;
            letterboxRect.offsetMin = Vector2.zero;
            letterboxRect.offsetMax = Vector2.zero;
            cinematicLetterbox = letterboxObject.AddComponent<CinematicLetterbox>();
        }

        Transform dialogueBoxObj = dialogueCanvas.transform.Find("DialogueBox");
        if (dialogueBoxObj != null) {
            cinematicLetterbox.transform.SetSiblingIndex(dialogueBoxObj.GetSiblingIndex());
        }
        else {
            cinematicLetterbox.transform.SetAsLastSibling();
        }

        cinematicLetterbox.EnsureBars();
        cinematicLetterbox.ApplyLayout();
    }

    private bool HasLetterbox() {
        return useCinematicLetterbox && cinematicLetterbox != null;
    }

    private void ShowLetterbox() {
        EnsureLetterbox();
        if (HasLetterbox()) cinematicLetterbox.Show();
    }

    private void ShowLetterboxImmediate() {
        EnsureLetterbox();
        if (HasLetterbox()) cinematicLetterbox.SetVisibleImmediate(true);
    }

    private void HideLetterboxImmediate() {
        if (cinematicLetterbox != null) cinematicLetterbox.SetVisibleImmediate(false);
    }

    private void DeactivateCanvasIfDialogueStillHidden() {
        if (!isShowing && dialogueCanvas != null) {
            dialogueCanvas.gameObject.SetActive(false);
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