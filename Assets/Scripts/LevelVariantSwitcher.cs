using UnityEngine;
using UnityEngine.InputSystem;

public class LevelVariantSwitcher : MonoBehaviour {
    [Header("Platform Variants")]
    [Tooltip("Element 0 is the blue platform group. Element 1 is the red platform group.")]
    [SerializeField] private GameObject[] variants;

    [Header("Input")]
    [SerializeField] private int currentIndex;

    [Header("Character Gate")]
    [Tooltip("Only the character with no double jump and no dash (the weaker character) is allowed to switch the world/platforms.")]
    [SerializeField] private bool restrictToWeakerCharacter = true;
    [SerializeField] private PlatformerPlayerController character;

    [Header("Red World Filter")]
    [Tooltip("切到红色世界时给整个画面加黑白滤镜。")]
    [SerializeField] private bool grayscaleOnRedVariant = true;

    [SerializeField] private bool createStartRoomDemoObject;

    private PlayerInputActions inputActions;
    private CharacterSwitcher2D characterSwitcher;
    private GrayscaleFilter grayscaleFilter;

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
        EnsureGrayscaleFilter();
        EnsureStartRoomDemoObject();
        ApplyVariant();
    }

    private void EnsureGrayscaleFilter() {
        if (!grayscaleOnRedVariant || grayscaleFilter != null) {
            return;
        }

        grayscaleFilter = FindFirstObjectByType<GrayscaleFilter>();

        if (grayscaleFilter != null) {
            return;
        }

        GameObject filterObject = new GameObject("WorldGrayscaleFilter");
        filterObject.transform.SetParent(transform, false);
        grayscaleFilter = filterObject.AddComponent<GrayscaleFilter>();
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

    // 有角色切换器时必须每次都问「当前是谁」，不能缓存。
    // 否则切到另一个角色后，这里还拿着已经被禁用的那个，能否切换世界就判错了。
    private void ResolveCharacterReference() {
        if (characterSwitcher == null) {
            characterSwitcher = FindFirstObjectByType<CharacterSwitcher2D>(FindObjectsInactive.Include);
        }

        if (characterSwitcher != null && characterSwitcher.CurrentCharacter != null) {
            character = characterSwitcher.CurrentCharacter;
            return;
        }

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

        ApplyRedWorldFilter();
    }

    private void ApplyRedWorldFilter() {
        if (!grayscaleOnRedVariant || grayscaleFilter == null) {
            return;
        }

        grayscaleFilter.SetActive(IsRedVariant);
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