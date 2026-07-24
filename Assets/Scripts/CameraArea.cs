using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraArea : MonoBehaviour {
    [Header("View Size")]
    [SerializeField] private float cameraSize = 5f;

    [Header("Starting Area (Optional)")]
    [SerializeField] private bool isStartingArea = false;

    public bool IsStartingArea => isStartingArea;

    [Header("Auto Trigger (Optional)")]
    [SerializeField] private bool enableAutoTrigger = false;
    [SerializeField] private bool autoTriggerSnapImmediate = false;
    [SerializeField] private float autoTriggerTransitionDuration = 0.35f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
