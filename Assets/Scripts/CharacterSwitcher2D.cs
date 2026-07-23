using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour {
    [Header("Characters")]
    [SerializeField] private PlatformerPlayerController poweredCharacter;
    [SerializeField] private PlatformerPlayerController basicCharacter;

    [Header("Input")]
    [SerializeField] private bool allowDirectInput = true;
    [SerializeField] private bool startInPoweredMode = true;

    [Header("Camera Transition")]
    [Tooltip("切换角色时摄像机移动到新角色的时长（秒）。设为 0 则瞬间硬切。")]
    [SerializeField] private float cameraTransitionDuration = 0.35f;
    [Tooltip("摄像机过渡的缓动曲线。留空则使用默认平滑。")]
    [SerializeField] private AnimationCurve cameraTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

    private void OnEnable() => inputActions?.Enable();
    private void OnDisable() => inputActions?.Disable();
    private void OnDestroy() => inputActions?.Dispose();

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
        if (!allowDirectInput || poweredCharacter == null || basicCharacter == null) return;

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
        // 明确分清谁是激活角色，谁是未激活角色
        PlatformerPlayerController activeChar = isPoweredMode ? poweredCharacter : basicCharacter;
        PlatformerPlayerController inactiveChar = isPoweredMode ? basicCharacter : poweredCharacter;

        if (activeChar == null) {
            Debug.LogError("【Switcher】当前激活的角色 (Active Character) 为空！请检查 Inspector 赋值！");
            return;
        }

        if (inactiveChar == null || inactiveChar == activeChar) {
            activeChar.gameObject.SetActive(true);
            activeChar.SetAbilities(isPoweredMode, isPoweredMode);
            RetargetCamera(activeChar);
            return;
        }

        if (syncPhysics) {
            activeChar.transform.position = inactiveChar.transform.position;
            activeChar.SyncStateFrom(inactiveChar);
        }

        // 核心修复顺序：先开启要切过去的角色，把摄像机挂上去，再关闭旧角色
        activeChar.gameObject.SetActive(true);
        activeChar.SetAbilities(isPoweredMode, isPoweredMode);

        Debug.Log($"【Switcher】正在切换：激活 [{activeChar.gameObject.name}]，关闭 [{inactiveChar.gameObject.name}]");
        RetargetCamera(activeChar);

        inactiveChar.gameObject.SetActive(false);
    }

    private void RetargetCamera(PlatformerPlayerController activeChar) {
        if (activeChar == null) return;

        PixelPerfectFollowCamera followCamera = Object.FindAnyObjectByType<PixelPerfectFollowCamera>();
        if (followCamera == null) followCamera = Object.FindObjectOfType<PixelPerfectFollowCamera>();

        if (followCamera != null) {
            // 强行把摄像机 Target 设为新角色的 Transform
            followCamera.SetTarget(activeChar.transform);
            followCamera.RefreshCameraBoundsToTarget(cameraTransitionDuration, cameraTransitionCurve);
            Debug.Log($"【Camera】成功将摄像机目标绑定到：{activeChar.transform.name}");
        }
        else {
            Debug.LogError("【Camera】找不到 PixelPerfectFollowCamera 脚本！");
        }
    }
}