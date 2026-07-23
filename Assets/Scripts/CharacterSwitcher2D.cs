using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour {
    [Header("Characters")]
    [SerializeField] private PlatformerPlayerController poweredCharacter;
    [SerializeField] private PlatformerPlayerController basicCharacter;

    [Header("Input")]
    [SerializeField] private bool allowDirectInput = true;
    [SerializeField] private bool startInPoweredMode = true;

    private bool isPoweredMode;
    private PlayerInputActions inputActions;

    public bool IsPoweredMode => isPoweredMode;
    public PlatformerPlayerController CurrentCharacter => isPoweredMode || basicCharacter == null ? poweredCharacter : basicCharacter;

    private void Awake() {
        inputActions = new PlayerInputActions();
        isPoweredMode = startInPoweredMode;

        if (poweredCharacter != null) {
            poweredCharacter.SetAbilities(true, true);
        }
        if (basicCharacter != null) {
            basicCharacter.SetAbilities(false, false);
        }

        ApplyCurrentMode(false);
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
        poweredCharacter = powered;
        basicCharacter = basic;
        ApplyCurrentMode(false);
    }

    public void Initialize(PlatformerPlayerController character) {
        poweredCharacter = character;
        basicCharacter = null;
        ApplyCurrentMode(false);
    }

    private void Update() {
        if (!allowDirectInput || poweredCharacter == null) {
            return;
        }

        if (inputActions.Player.SwitchCharacter.WasPressedThisFrame()) {
            TogglePoweredMode();
        }
    }

    public void SetPoweredMode(bool powered) {
        if (isPoweredMode == powered) return;
        isPoweredMode = powered;
        ApplyCurrentMode(true);
        AudioManager.Instance?.PlayOneShot(SoundType.CharacterSwitch);
    }

    public void TogglePoweredMode() {
        SetPoweredMode(!isPoweredMode);
    }

    private void ApplyCurrentMode(bool syncPhysics) {
        PlatformerPlayerController activeChar = isPoweredMode ? poweredCharacter : basicCharacter;
        PlatformerPlayerController inactiveChar = isPoweredMode ? basicCharacter : poweredCharacter;

        if (activeChar == null) {
            return;
        }

        if (inactiveChar == null || inactiveChar == activeChar) {
            activeChar.gameObject.SetActive(true);
            activeChar.SetAbilities(isPoweredMode, isPoweredMode);
            return;
        }

        if (syncPhysics) {
            activeChar.transform.position = inactiveChar.transform.position;
            activeChar.SyncStateFrom(inactiveChar);
        }

        activeChar.gameObject.SetActive(true);
        inactiveChar.gameObject.SetActive(false);

        activeChar.SetAbilities(isPoweredMode, isPoweredMode);
    }
}
