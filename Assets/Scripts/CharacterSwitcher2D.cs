using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour {
    [Header("Character")]
    [SerializeField] private PlatformerPlayerController character;

    [Header("Modes")]
    [SerializeField] private RuntimeAnimatorController poweredAnimatorController;
    [SerializeField] private RuntimeAnimatorController basicAnimatorController;
    [SerializeField] private bool poweredCanDoubleJump = true;
    [SerializeField] private bool poweredCanDash = true;
    [SerializeField] private bool basicCanDoubleJump;
    [SerializeField] private bool basicCanDash;

    [Header("Input")]
    [SerializeField] private bool allowDirectInput = true;
    [SerializeField] private bool startInPoweredMode = true;

    private bool isPoweredMode;
    private PlayerInputActions inputActions;

    public bool IsPoweredMode => isPoweredMode;
    public PlatformerPlayerController CurrentCharacter => character;

    private void Awake() {
        inputActions = new PlayerInputActions();
        isPoweredMode = startInPoweredMode;

        if (character == null) {
            character = GetComponent<PlatformerPlayerController>();
        }

        ApplyCurrentMode();
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

    public void Initialize(PlatformerPlayerController powered, PlatformerPlayerController basic) {
        character = powered != null ? powered : basic;
        ApplyCurrentMode();
    }

    public void Initialize(PlatformerPlayerController targetCharacter) {
        character = targetCharacter;
        ApplyCurrentMode();
    }

    private void Update() {
        if (!allowDirectInput || character == null) {
            return;
        }

        if (inputActions.Player.SwitchCharacter.WasPressedThisFrame()) {
            TogglePoweredMode();
        }
    }

    public void SetPoweredMode(bool powered) {
        if (isPoweredMode == powered) {
            return;
        }

        isPoweredMode = powered;
        ApplyCurrentMode();
        AudioManager.Instance?.PlayOneShot(SoundType.CharacterSwitch);
    }

    public void TogglePoweredMode() {
        SetPoweredMode(!isPoweredMode);
    }

    private void ApplyCurrentMode() {
        if (character == null) {
            return;
        }

        character.gameObject.SetActive(true);
        character.SetAbilities(
            isPoweredMode ? poweredCanDoubleJump : basicCanDoubleJump,
            isPoweredMode ? poweredCanDash : basicCanDash);
        character.SetAnimatorController(isPoweredMode ? poweredAnimatorController : basicAnimatorController);
        RetargetCamera(character);
    }

    private void RetargetCamera(PlatformerPlayerController activeChar) {
        if (activeChar == null) {
            return;
        }

        PixelPerfectFollowCamera followCamera =
            FindFirstObjectByType<PixelPerfectFollowCamera>();

        if (followCamera == null) {
            return;
        }

        followCamera.SetTarget(activeChar.transform);
    }
}
