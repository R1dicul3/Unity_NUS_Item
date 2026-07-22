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

    public Transform Target => target;

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
        if (target == null) return;
        ApplyFollow();
    }

    private void ApplyFollow() {
        UpdateOrthoSize();

        Vector2 desiredPosition = (Vector2)target.position + followOffset;

        if (clampToBounds && hasBounds) {
            desiredPosition = ClampToCameraEdges(desiredPosition);
        }

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
