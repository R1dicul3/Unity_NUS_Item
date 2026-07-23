using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelPerfectFollowCamera : MonoBehaviour {
    [Header("Follow Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 followOffset = Vector2.zero;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.12f;
    [SerializeField] private float orthoSizeSmoothTime = 0.2f;

    [Header("Pixel Snapping")]
    [Tooltip("Pixels per Unity unit. Keep this aligned with sprite Pixels Per Unit.")]
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private bool snapToPixelGrid = true;

    [Header("Bounds")]
    [SerializeField] private bool clampToBounds = true;

    private Camera cam;
    private Vector2 positionVelocity;
    private float orthoSizeVelocity;

    private Bounds currentBounds;
    private bool hasBounds;

    private float targetOrthoSize;

    private bool forceSnapNextUpdate;

    private Coroutine transitionRoutine;
    private bool isTransitioning;

    public Transform Target => target;
    public bool IsTransitioning => isTransitioning;

    private void Awake() {
        cam = GetComponent<Camera>();
        if (!cam.orthographic) {
            Debug.LogWarning($"{nameof(PixelPerfectFollowCamera)} requires an orthographic camera.");
        }
        targetOrthoSize = cam.orthographicSize;
    }

    private void OnEnable() {
        forceSnapNextUpdate = true;
    }

    private void OnDisable() {
        // 禁用时协程会被 Unity 直接终止，这里必须手动清掉过渡状态，
        // 否则重新启用后 LateUpdate 会一直以为还在过渡中而不再跟随。
        StopTransition();
    }

    private void Start() {
        if (hasBounds) return;

        CameraArea[] areas = FindObjectsByType<CameraArea>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (CameraArea area in areas) {
            if (!area.IsStartingArea) continue;

            SetCameraBounds(area.CameraBounds);
            SetCameraSize(area.CameraSize);
            SnapImmediate();
            break;
        }
    }

    private void LateUpdate() {
        if (target == null || isTransitioning) return;
        ApplyFollow();
    }

    private void ApplyFollow() {
        UpdateOrthoSize();

        Vector2 desiredPosition = GetDesiredPosition();

        Vector2 newPosition;
        if (forceSnapNextUpdate) {
            newPosition = desiredPosition;
            positionVelocity = Vector2.zero;
        }
        else {
            newPosition = Vector2.SmoothDamp(
                (Vector2)transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime
            );
        }

        if (snapToPixelGrid) {
            newPosition = SnapToPixelGrid(newPosition);
        }

        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        forceSnapNextUpdate = false;
    }

    private void UpdateOrthoSize() {
        if (forceSnapNextUpdate) {
            cam.orthographicSize = targetOrthoSize;
            orthoSizeVelocity = 0f;
            return;
        }

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetOrthoSize,
            ref orthoSizeVelocity,
            orthoSizeSmoothTime
        );
    }

    private Vector2 GetDesiredPosition() {
        if (target == null) return transform.position;

        Vector2 desiredPosition = (Vector2)target.position + followOffset;

        if (clampToBounds && hasBounds) {
            desiredPosition = ClampToCameraEdges(desiredPosition);
        }

        return desiredPosition;
    }

    private Vector2 ClampToCameraEdges(Vector2 desiredPosition) {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = currentBounds.min.x + halfWidth;
        float maxX = currentBounds.max.x - halfWidth;
        float minY = currentBounds.min.y + halfHeight;
        float maxY = currentBounds.max.y - halfHeight;

        float x = minX <= maxX
            ? Mathf.Clamp(desiredPosition.x, minX, maxX)
            : currentBounds.center.x;

        float y = minY <= maxY
            ? Mathf.Clamp(desiredPosition.y, minY, maxY)
            : currentBounds.center.y;

        return new Vector2(x, y);
    }

    private Vector2 SnapToPixelGrid(Vector2 position) {
        if (pixelsPerUnit <= 0) return position;
        float unitPerPixel = 1f / pixelsPerUnit;
        return new Vector2(
            Mathf.Round(position.x / unitPerPixel) * unitPerPixel,
            Mathf.Round(position.y / unitPerPixel) * unitPerPixel
        );
    }

    public void SetTarget(Transform newTarget) {
        target = newTarget;
    }

    public void SetCameraBounds(Bounds bounds) {
        currentBounds = bounds;
        hasBounds = true;
    }

    public void ClearCameraBounds() {
        hasBounds = false;
    }

    public void SetCameraSize(float orthographicSize) {
        targetOrthoSize = orthographicSize;
    }

    public void SnapImmediate() {
        forceSnapNextUpdate = true;
        if (target != null) {
            ApplyFollow();
        }
    }

    /// <summary>
    /// 平滑地把摄像机移动/缩放到新的房间区域，代替 SetCameraBounds + SetCameraSize + SnapImmediate 的硬切。
    /// duration 小于等于 0 时退化为瞬间切换，行为与 SnapImmediate 完全一致。
    /// </summary>
    public void TransitionTo(
        Bounds bounds,
        float orthographicSize,
        float duration,
        AnimationCurve easing = null
    ) {
        StopTransition();

        if (duration <= 0f || target == null || !isActiveAndEnabled) {
            SetCameraBounds(bounds);
            SetCameraSize(orthographicSize);
            SnapImmediate();
            return;
        }

        transitionRoutine = StartCoroutine(
            TransitionRoutine(bounds, orthographicSize, duration, easing)
        );
    }

    public void StopTransition() {
        if (transitionRoutine != null) {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isTransitioning = false;
    }

    private IEnumerator TransitionRoutine(
        Bounds bounds,
        float orthographicSize,
        float duration,
        AnimationCurve easing
    ) {
        Vector2 startPosition = transform.position;
        float startOrthoSize = cam.orthographicSize;

        // 先切边界和目标视野，这样每帧重算的落点就是新房间的落点。
        SetCameraBounds(bounds);
        SetCameraSize(orthographicSize);

        isTransitioning = true;
        positionVelocity = Vector2.zero;
        orthoSizeVelocity = 0f;

        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = easing != null && easing.length > 0
                ? easing.Evaluate(progress)
                : Mathf.SmoothStep(0f, 1f, progress);

            // 视野先插值，GetDesiredPosition 的边界夹取依赖当前 orthographicSize。
            cam.orthographicSize = Mathf.Lerp(
                startOrthoSize,
                targetOrthoSize,
                easedProgress
            );

            // 玩家在过渡期间仍可移动，所以落点每帧重算。
            Vector2 newPosition = Vector2.Lerp(
                startPosition,
                GetDesiredPosition(),
                easedProgress
            );

            if (snapToPixelGrid) {
                newPosition = SnapToPixelGrid(newPosition);
            }

            transform.position = new Vector3(
                newPosition.x,
                newPosition.y,
                transform.position.z
            );

            yield return null;
        }

        transitionRoutine = null;
        isTransitioning = false;

        SnapImmediate();
    }

    public void ForceSnapToTarget() {
        SnapImmediate();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if (!hasBounds) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(currentBounds.center, currentBounds.size);
    }
#endif
}
