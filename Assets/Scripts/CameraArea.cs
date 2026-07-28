using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraArea : MonoBehaviour {
    [Header("视野大小")]
    [SerializeField] private float cameraSize = 5f;

    [Header("出生房间（可选）")]
    [SerializeField] private bool isStartingArea = false;

    public bool IsStartingArea => isStartingArea;

    [Header("自动触发（可选）")]
    [SerializeField] private bool enableAutoTrigger = false;
    [SerializeField] private bool autoTriggerSnapImmediate = false;
    [SerializeField] private float autoTriggerTransitionDuration = 0.35f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("房间音乐")]
    [Tooltip("进入该房间时播放的 BGM。设为 None 则保持当前音乐不变。")]
    [SerializeField] private SoundType areaMusic = SoundType.None;

    private BoxCollider2D boundsCollider;

    public Bounds CameraBounds => boundsCollider.bounds;
    public float CameraSize => cameraSize;
    public SoundType AreaMusic => areaMusic;

    private void Awake() {
        boundsCollider = GetComponent<BoxCollider2D>();
        boundsCollider.isTrigger = true;
    }

    private void Reset() {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!enableAutoTrigger) return;

        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null || !player.IsControlled) return;

        PixelPerfectFollowCamera camera = FindFirstObjectByType<PixelPerfectFollowCamera>();
        if (camera == null) return;

        if (autoTriggerSnapImmediate) {
            camera.SetCameraBounds(CameraBounds);
            camera.SetCameraSize(CameraSize);
            camera.SnapImmediate();
        }
        else if (autoTriggerTransitionDuration > 0f) {
            camera.TransitionTo(CameraBounds, CameraSize, autoTriggerTransitionDuration, transitionCurve);
        }
        else {
            camera.SetCameraBounds(CameraBounds);
            camera.SetCameraSize(CameraSize);
        }

        if (areaMusic != SoundType.None) {
            AudioManager.Instance?.PlayMusic(areaMusic);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
#endif
}