using UnityEngine;
using UnityEngine.InputSystem;

// 只负责"切换角色"：能力（二段跳/冲刺）和颜色。
// 完全独立于世界/平台切换（LevelVariantSwitcher），互不影响。
public class CharacterSwitcher2D : MonoBehaviour {
    [Header("Target")]
    [SerializeField] private PlatformerPlayerController character;

    [Header("Input")]
    [SerializeField] private bool allowDirectInput = true;
    [SerializeField] private bool startInPoweredMode = true;

    [Header("Modes")]
    [SerializeField] private Color poweredColor = new Color(1f, 0.05f, 0.72f);
    [SerializeField] private Color basicColor = new Color(0.05f, 1f, 0.2f);

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

    private void OnDestroy() {
        inputActions?.Dispose();
    }

    public void Initialize(PlatformerPlayerController playableCharacter) {
        character = playableCharacter;
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

        character.SetAbilities(isPoweredMode, isPoweredMode, isPoweredMode ? poweredColor : basicColor);
    }
}
