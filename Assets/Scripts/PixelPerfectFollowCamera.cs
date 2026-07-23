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
    [SerializeField] private bool clampToRoomMarkers = true;
    [SerializeField] private string roomMarkerRootName = "White_Box";

    private Camera cam;
    private Vector2 positionVelocity;
    private float orthoSizeVelocity;

    private Bounds currentBounds;
    private bool hasBounds;

    private float targetOrthoSize;

    private bool forceSnapNextUpdate;

    private Coroutine transitionRoutine;
    private bool isTransitioning;
    private Transform roomMarkerRoot;

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
        RoomDoor.PlayerTeleported += HandlePlayerTeleported;
        forceSnapNextUpdate = true;
    }

    private void OnDisable() {
        RoomDoor.PlayerTeleported -= HandlePlayerTeleported;
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

        if (!hasBounds && clampToRoomMarkers) {
            SetCurrentRoomMarkerFromTarget();
            SnapImmediate();
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

    private void HandlePlayerTeleported(
        PlatformerPlayerController player,
        RoomDoor sourceDoor,
        RoomDoor targetDoor
    ) {
        if (!clampToRoomMarkers ||
            player == null ||
            target == null ||
            player.transform != target) {
            return;
        }

        string targetAreaName =
            targetDoor != null ? targetDoor.GetAreaName() : string.Empty;

        if (!string.IsNullOrEmpty(targetAreaName) &&
            TrySetBoundsFromRoomMarker(targetAreaName)) {
            SnapImmediate();
            return;
        }

        SetCurrentRoomMarkerFromTarget();
        SnapImmediate();
    }

    private void SetCurrentRoomMarkerFromTarget() {
        if (target == null) return;
        SetCurrentRoomMarkerFromPosition(target.position);
    }

    private void SetCurrentRoomMarkerFromPosition(Vector3 position) {
        CacheRoomMarkerRoot();
        if (roomMarkerRoot == null) return;

        Transform bestMarker = null;
        Bounds bestBounds = default;
        float bestDistance = float.MaxValue;

        foreach (Transform marker in EnumerateRoomMarkers()) {
            if (!IsAreaName(marker.name) ||
                !TryCalculateMarkerBounds(marker, out Bounds markerBounds)) {
                continue;
            }

            if (Contains2D(markerBounds, position)) {
                SetCameraBounds(markerBounds);
                return;
            }

            float distance = DistanceToBounds2D(markerBounds, position);
            if (distance < bestDistance) {
                bestDistance = distance;
                bestMarker = marker;
                bestBounds = markerBounds;
            }
        }

        if (bestMarker != null) {
            SetCameraBounds(bestBounds);
        }
    }

    private bool TrySetBoundsFromRoomMarker(string markerName) {
        CacheRoomMarkerRoot();
        if (roomMarkerRoot == null) return false;

        foreach (Transform marker in EnumerateRoomMarkers()) {
            if (marker.name != markerName) continue;
            if (!TryCalculateMarkerBounds(marker, out Bounds markerBounds)) {
                return false;
            }

            SetCameraBounds(markerBounds);
            return true;
        }

        return false;
    }

    private void CacheRoomMarkerRoot() {
        if (roomMarkerRoot != null && HasAreaChildren(roomMarkerRoot)) {
            return;
        }

        GameObject root = GameObject.Find(roomMarkerRootName);
        roomMarkerRoot = root != null ? root.transform : null;
    }

    private System.Collections.Generic.IEnumerable<Transform> EnumerateRoomMarkers() {
        if (roomMarkerRoot == null) yield break;

        Transform areaRoot = roomMarkerRoot.Find("Area_Markers");
        if (areaRoot == null) {
            areaRoot = roomMarkerRoot;
        }

        foreach (Transform child in areaRoot) {
            yield return child;
        }
    }

    private static bool TryCalculateMarkerBounds(
        Transform marker,
        out Bounds bounds
    ) {
        bounds = default;

        SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
        if (markerRenderer == null || markerRenderer.sprite == null) {
            return false;
        }

        Bounds spriteBounds = markerRenderer.sprite.bounds;
        Vector3 scaledSize = Vector3.Scale(
            spriteBounds.size,
            marker.lossyScale
        );
        scaledSize = new Vector3(
            Mathf.Abs(scaledSize.x),
            Mathf.Abs(scaledSize.y),
            Mathf.Abs(scaledSize.z)
        );

        if (scaledSize.x <= 0.001f || scaledSize.y <= 0.001f) {
            return false;
        }

        bounds = new Bounds(
            marker.TransformPoint(spriteBounds.center),
            scaledSize
        );
        return true;
    }

    private static bool HasAreaChildren(Transform root) {
        Transform areaRoot = root.Find("Area_Markers");
        if (areaRoot == null) {
            areaRoot = root;
        }

        foreach (Transform child in areaRoot) {
            if (IsAreaName(child.name)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsAreaName(string objectName) {
        return objectName == "Room_Start" ||
               objectName.StartsWith("Room_") ||
               objectName.StartsWith("Path_");
    }

    private static bool Contains2D(Bounds bounds, Vector3 position) {
        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.y >= bounds.min.y &&
               position.y <= bounds.max.y;
    }

    private static float DistanceToBounds2D(Bounds bounds, Vector3 position) {
        float dx = Mathf.Max(
            bounds.min.x - position.x,
            0f,
            position.x - bounds.max.x
        );
        float dy = Mathf.Max(
            bounds.min.y - position.y,
            0f,
            position.y - bounds.max.y
        );
        return dx * dx + dy * dy;
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

            cam.orthographicSize = Mathf.Lerp(
                startOrthoSize,
                targetOrthoSize,
                easedProgress
            );

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

    public void RefreshCameraBoundsToTarget(float transitionDuration = 0f, AnimationCurve curve = null) {
        if (target == null) return;

        // 强制同步物理，确保新激活的角色碰撞体能被正确检测
        Physics2D.SyncTransforms();

        Collider2D[] hits = Physics2D.OverlapPointAll(target.position);
        CameraArea targetArea = null;

        foreach (Collider2D hit in hits) {
            targetArea = hit.GetComponent<CameraArea>();
            if (targetArea != null) break;
        }

        if (targetArea != null) {
            if (transitionDuration > 0f) {
                TransitionTo(targetArea.CameraBounds, targetArea.CameraSize, transitionDuration, curve);
            }
            else {
                SetCameraBounds(targetArea.CameraBounds);
                SetCameraSize(targetArea.CameraSize);
                SnapImmediate();
            }
            return;
        }

        // 清空旧边界，防止被锁死在旧房间
        ClearCameraBounds();

        if (clampToRoomMarkers) {
            SetCurrentRoomMarkerFromTarget();
            if (hasBounds) {
                if (transitionDuration > 0f) {
                    TransitionTo(currentBounds, targetOrthoSize, transitionDuration, curve);
                }
                else {
                    SnapImmediate();
                }
                return;
            }
        }

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