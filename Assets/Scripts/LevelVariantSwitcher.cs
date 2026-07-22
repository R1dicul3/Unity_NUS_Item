using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelVariantSwitcher : MonoBehaviour {
    [Header("Platform Variants")]
    [Tooltip("Element 0 is the blue platform group. Element 1 is the red platform group.")]
    [SerializeField] private GameObject[] variants;
    [SerializeField] private Color bluePlatformColor = new Color(0.08f, 0.42f, 1f, 1f);
    [SerializeField] private Color redPlatformColor = new Color(0.95f, 0.1f, 0.12f, 1f);

    [Header("Input")]
    [SerializeField] private int currentIndex;

    [Header("Character Gate")]
    [Tooltip("Only the character with no double jump and no dash (the weaker character) is allowed to switch the world/platforms.")]
    [SerializeField] private bool restrictToWeakerCharacter = true;
    [SerializeField] private PlatformerPlayerController character;

    [Header("Indicator")]
    [SerializeField] private Text variantLabel;
    [SerializeField] private int labelFontSize = 96;
    [SerializeField] private Vector2 labelOffset = new Vector2(48f, -36f);
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private bool createStartRoomDemoObject;

    private PlayerInputActions inputActions;

    public int CurrentIndex => currentIndex;
    public bool IsRedVariant => currentIndex == 1;

    private void Awake() {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable() {
        inputActions?.Enable();
    }

    private void OnDisable() {
        inputActions?.Disable();
    }

    private void OnDestroy() {
        inputActions?.Dispose();
    }

    private void Start() {
        ResolveCharacterReference();
        EnsureVariantLabel();
        EnsureStartRoomDemoObject();
        ApplyVariant();
    }

    private void Update() {
        EnforceCharacterGate();

        if (variants == null || variants.Length < 2) {
            return;
        }

        if (inputActions.Player.SwitchVariant.WasPressedThisFrame() && CanSwitchWorld()) {
            currentIndex = currentIndex == 0 ? 1 : 0;
            ApplyVariant();
            AudioManager.Instance?.PlayOneShot(SoundType.SwitchVariant);
        }
    }

    // Keep the red variant gated to the weaker character, including after a character switch.
    private void EnforceCharacterGate() {
        if (currentIndex == 1 && !CanSwitchWorld()) {
            currentIndex = 0;
            ApplyVariant();
        }
    }

    public bool CanSwitchWorld() {
        if (!restrictToWeakerCharacter) {
            return true;
        }

        ResolveCharacterReference();
        if (character == null) {
            return true;
        }

        return character.IsWeakerCharacter;
    }

    private void ResolveCharacterReference() {
        if (character == null) {
            character = FindFirstObjectByType<PlatformerPlayerController>(FindObjectsInactive.Include);
        }
    }

    public void SetVariant(int index) {
        if (variants == null || variants.Length < 2) {
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, 1);
        ApplyVariant();
    }

    private void ApplyVariant() {
        if (variants == null) {
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, 1);
        for (int i = 0; i < variants.Length; i++) {
            if (variants[i] != null) {
                variants[i].SetActive(i == currentIndex);
            }
        }

        TintPlatformGroup(variants.Length > 0 ? variants[0] : null, bluePlatformColor);
        TintPlatformGroup(variants.Length > 1 ? variants[1] : null, redPlatformColor);
        UpdateVariantLabel();
    }

    private static void TintPlatformGroup(GameObject root, Color color) {
        if (root == null) {
            return;
        }

        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) {
            renderer.color = color;
        }
    }

    private void EnsureVariantLabel() {
        if (variantLabel != null) {
            ConfigureVariantLabel();
            return;
        }

        GameObject canvasObject = new GameObject("LevelVariantIndicatorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject labelObject = new GameObject("LevelVariantLabel", typeof(Text));
        labelObject.transform.SetParent(canvasObject.transform, false);
        variantLabel = labelObject.GetComponent<Text>();
        ConfigureVariantLabel();
    }

    private void ConfigureVariantLabel() {
        variantLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        variantLabel.fontSize = labelFontSize;
        variantLabel.fontStyle = FontStyle.Bold;
        variantLabel.color = labelColor;
        variantLabel.alignment = TextAnchor.UpperLeft;
        variantLabel.raycastTarget = false;

        RectTransform labelTransform = variantLabel.rectTransform;
        labelTransform.anchorMin = new Vector2(0f, 1f);
        labelTransform.anchorMax = new Vector2(0f, 1f);
        labelTransform.pivot = new Vector2(0f, 1f);
        labelTransform.anchoredPosition = labelOffset;
        labelTransform.sizeDelta = new Vector2(420f, 130f);
    }

    private void UpdateVariantLabel() {
        if (variantLabel == null) {
            return;
        }

        variantLabel.text = IsRedVariant ? "RED" : "BLUE";
    }

    private void EnsureStartRoomDemoObject() {
        if (!createStartRoomDemoObject || FindFirstObjectByType<LevelVariantDemoObject>() != null) {
            return;
        }

        GameObject startRoom = GameObject.Find("Room_Start");
        if (startRoom == null) {
            return;
        }

        LevelVariantDemoObject.Create(startRoom.transform, this);
    }
}
