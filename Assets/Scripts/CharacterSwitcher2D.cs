using UnityEngine;
using UnityEngine.InputSystem;

// 只负责"切换角色"：能力（二段跳/冲刺）、颜色、动画控制器。
// 完全独立于世界/平台切换（LevelVariantSwitcher），互不影响。
public class CharacterSwitcher2D : MonoBehaviour {
    private const string DefaultBasicControllerResourcePath = "Animations/ManBasic";

    [Header("Target")]
    [SerializeField] private PlatformerPlayerController character;

    [Header("Input")]
    [SerializeField] private bool allowDirectInput = true;
    [SerializeField] private bool startInPoweredMode = true;

    [Header("Modes")]
    [SerializeField] private Color poweredColor = new Color(1f, 0.05f, 0.72f);
    [SerializeField] private Color basicColor = new Color(0.05f, 1f, 0.2f);

    [Header("Animation Placeholders")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private RuntimeAnimatorController poweredAnimationPlaceholder;
    [SerializeField] private RuntimeAnimatorController basicAnimationPlaceholder;

    private bool isPoweredMode;
    private PlayerInputActions inputActions;

    public bool IsPoweredMode => isPoweredMode;

    private void Awake() {
        inputActions = new PlayerInputActions();
        isPoweredMode = startInPoweredMode;
        ApplyCurrentMode();
    }

    private void OnEnable() {
        inputActions?.Enable();
    }

    private void OnDisable() {
        inputActions?.Disable();
    }

    public void Initialize(PlatformerPlayerController playableCharacter) {
        character = playableCharacter;
        ResolveAnimatorIfNeeded();
        ApplyCurrentMode();
    }

    private void Update() {
        if (!allowDirectInput || character == null) {
            return;
        }

        if (inputActions.Player.SwitchCharacter.WasPressedThisFrame()) {
            SetPoweredMode(!isPoweredMode);
        }
    }

    public void SetPoweredMode(bool powered) {
        isPoweredMode = powered;
        ApplyCurrentMode();
    }

    public void TogglePoweredMode() {
        SetPoweredMode(!isPoweredMode);
    }

    private void ApplyCurrentMode() {
        if (character == null) {
            return;
        }

        RuntimeAnimatorController controller = GetControllerForCurrentMode();
        Color activeColor = isPoweredMode || controller == null ? (isPoweredMode ? poweredColor : basicColor) : Color.white;
        character.SetAbilities(isPoweredMode, isPoweredMode, activeColor);
        ApplyAnimator(controller);
    }

    private RuntimeAnimatorController GetControllerForCurrentMode() {
        if (!isPoweredMode && basicAnimationPlaceholder == null) {
            basicAnimationPlaceholder = Resources.Load<RuntimeAnimatorController>(DefaultBasicControllerResourcePath);
        }

        return isPoweredMode
            ? poweredAnimationPlaceholder
            : basicAnimationPlaceholder;
    }

    private void ApplyAnimator(RuntimeAnimatorController controller) {
        ResolveAnimatorIfNeeded();
        if (characterAnimator == null) {
            return;
        }

        if (characterAnimator.runtimeAnimatorController != controller) {
            characterAnimator.runtimeAnimatorController = controller;
        }

        if (controller != null) {
            characterAnimator.SetLayerWeight(0, 1f);
            characterAnimator.Rebind();
            characterAnimator.Update(0f);
        }
    }

    private void ResolveAnimatorIfNeeded() {
        if (characterAnimator == null && character != null) {
            characterAnimator = character.GetComponentInChildren<Animator>(true);
        }
    }
}
