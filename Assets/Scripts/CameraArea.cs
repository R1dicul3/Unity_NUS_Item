using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraArea : MonoBehaviour {
    [Header("视野大小")]
    [Tooltip("这个区域使用的正交摄像机 Size，一般像素游戏里所有房间用同一个值即可")]
    [SerializeField] private float cameraSize = 5f;

    [Header("出生房间（可选）")]
    [Tooltip("勾选后，场景一开始加载摄像机就会立刻用这个区域的边界/视野初始化，" +
             "不用等玩家第一次走门。玩家出生的那个房间勾选这个。")]
    [SerializeField] private bool isStartingArea = false;

    public bool IsStartingArea => isStartingArea;

    [Header("自动触发（可选）")]
    [Tooltip("勾选后，玩家走进该区域会自动切换摄像机边界，无需通过 RoomDoor 传送门")]
    [SerializeField] private bool enableAutoTrigger = false;
    [Tooltip("自动触发时是否瞬间切换（不平滑）。同一场景内连续行走建议关闭，做成平滑跟随更自然")]
    [SerializeField] private bool autoTriggerSnapImmediate = false;

    private BoxCollider2D boundsCollider;

    public Bounds CameraBounds => boundsCollider.bounds;
    public float CameraSize => cameraSize;

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
        if (player == null) return;

        PixelPerfectFollowCamera camera = FindFirstObjectByType<PixelPerfectFollowCamera>();
        if (camera == null) return;

        camera.SetCameraBounds(CameraBounds);
        camera.SetCameraSize(CameraSize);

        if (autoTriggerSnapImmediate) {
            camera.SnapImmediate();
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
