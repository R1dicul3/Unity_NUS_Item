using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(2f, 1.2f);
    [SerializeField] private float smoothTime = 0.16f;
    [SerializeField] private float minY = -1.5f;
    [SerializeField] private bool clampToCurrentRoom = true;
    [SerializeField] private bool snapOnRoomChange = true;
    [SerializeField] private string whiteboxRootName = "White_Box";

    private Vector3 velocity;
    private Camera followCamera;
    private Transform whiteboxRoot;
    private Transform currentArea;
    private Bounds currentAreaBounds;
    private bool hasCurrentAreaBounds;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        if (isActiveAndEnabled)
        {
            InitializeCurrentArea();
        }
    }

    /// <summary>
    /// 强制相机立即跳转到目标位置，重置平滑速度。
    /// 用于加载存档后立刻刷新摄像机。
    /// </summary>
    public void ForceSnapToTarget()
    {
        velocity = Vector3.zero;
        FindTargetIfMissing();
        if (target == null)
        {
            return;
        }
        InitializeCurrentArea(forceFromTargetPosition: true);
        Vector3 desired = GetDesiredCameraPosition();
        transform.position = clampToCurrentRoom ? ClampToCurrentArea(desired) : desired;
    }

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
        CacheWhiteboxRoot();
    }

    private void OnEnable()
    {
        RoomDoor.PlayerTeleported += HandlePlayerTeleported;
        InitializeCurrentArea();
    }

    private void OnDisable()
    {
        RoomDoor.PlayerTeleported -= HandlePlayerTeleported;
    }

    private void Start()
    {
        FindTargetIfMissing();
        InitializeCurrentArea();
    }

    private void LateUpdate()
    {
        FindTargetIfMissing();
        if (target == null)
        {
            return;
        }

        Vector3 desired = GetDesiredCameraPosition();
        if (clampToCurrentRoom)
        {
            EnsureCurrentArea();
            desired = ClampToCurrentArea(desired);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private void HandlePlayerTeleported(PlatformerPlayerController player, RoomDoor sourceDoor, RoomDoor targetDoor)
    {
        if (target == null || player == null || player.transform != target)
        {
            return;
        }

        string targetAreaName = targetDoor != null ? targetDoor.GetSourceAreaName() : string.Empty;
        if (!string.IsNullOrEmpty(targetAreaName) && TrySetCurrentArea(targetAreaName))
        {
            SnapToTargetIfNeeded();
            return;
        }

        InitializeCurrentArea(forceFromTargetPosition: true);
        SnapToTargetIfNeeded();
    }

    private void InitializeCurrentArea(bool forceFromTargetPosition = false)
    {
        if (!clampToCurrentRoom || target == null)
        {
            return;
        }

        CacheWhiteboxRoot();
        if (whiteboxRoot == null)
        {
            return;
        }

        if (forceFromTargetPosition || currentArea == null || !currentArea.gameObject.activeInHierarchy)
        {
            SetCurrentAreaFromPosition(target.position);
            return;
        }
    }

    private void EnsureCurrentArea()
    {
        if (hasCurrentAreaBounds)
        {
            return;
        }

        SetCurrentAreaFromPosition(target.position);
    }

    private bool TrySetCurrentArea(string areaName)
    {
        CacheWhiteboxRoot();
        if (whiteboxRoot == null)
        {
            return false;
        }

        Transform area = whiteboxRoot.Find(areaName);
        if (area == null || !TryCalculateAreaBounds(area, out Bounds areaBounds))
        {
            return false;
        }

        currentArea = area;
        currentAreaBounds = areaBounds;
        hasCurrentAreaBounds = true;
        return true;
    }

    private void SetCurrentAreaFromPosition(Vector3 position)
    {
        CacheWhiteboxRoot();
        if (whiteboxRoot == null)
        {
            return;
        }

        Transform bestArea = null;
        Bounds bestBounds = default;
        float bestDistance = float.MaxValue;

        foreach (Transform child in whiteboxRoot)
        {
            if (!IsAreaName(child.name) || !TryCalculateAreaBounds(child, out Bounds areaBounds))
            {
                continue;
            }

            if (Contains2D(areaBounds, position))
            {
                currentArea = child;
                currentAreaBounds = areaBounds;
                hasCurrentAreaBounds = true;
                return;
            }

            float distance = DistanceToBounds2D(areaBounds, position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestArea = child;
                bestBounds = areaBounds;
            }
        }

        if (bestArea != null)
        {
            currentArea = bestArea;
            currentAreaBounds = bestBounds;
            hasCurrentAreaBounds = true;
        }
    }

    private Vector3 ClampToCurrentArea(Vector3 desired)
    {
        if (!hasCurrentAreaBounds || followCamera == null || !followCamera.orthographic)
        {
            return desired;
        }

        float halfHeight = followCamera.orthographicSize;
        float halfWidth = halfHeight * followCamera.aspect;
        float minX = currentAreaBounds.min.x + halfWidth;
        float maxX = currentAreaBounds.max.x - halfWidth;
        float minYBound = currentAreaBounds.min.y + halfHeight;
        float maxYBound = currentAreaBounds.max.y - halfHeight;

        desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : currentAreaBounds.center.x;
        desired.y = minYBound <= maxYBound ? Mathf.Clamp(desired.y, minYBound, maxYBound) : currentAreaBounds.center.y;
        return desired;
    }

    private void SnapToTargetIfNeeded()
    {
        velocity = Vector3.zero;
        if (!snapOnRoomChange || target == null)
        {
            return;
        }

        Vector3 desired = GetDesiredCameraPosition();
        transform.position = clampToCurrentRoom ? ClampToCurrentArea(desired) : desired;
    }

    private Vector3 GetDesiredCameraPosition()
    {
        float desiredY = target.position.y + offset.y;
        if (!clampToCurrentRoom || !hasCurrentAreaBounds)
        {
            desiredY = Mathf.Max(desiredY, minY);
        }

        return new Vector3(target.position.x + offset.x, desiredY, transform.position.z);
    }

    private bool TryCalculateAreaBounds(Transform area, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        BoxCollider2D[] colliders = area.GetComponentsInChildren<BoxCollider2D>(true);

        foreach (BoxCollider2D areaCollider in colliders)
        {
            if (areaCollider.isTrigger || areaCollider.GetComponent<RoomDoor>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = areaCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(areaCollider.bounds);
            }
        }

        return hasBounds;
    }

    private void CacheWhiteboxRoot()
    {
        if (whiteboxRoot != null)
        {
            return;
        }

        GameObject root = GameObject.Find(whiteboxRootName);
        whiteboxRoot = root != null ? root.transform : null;
    }

    private void FindTargetIfMissing()
    {
        if (target != null)
        {
            return;
        }

        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    private static bool IsAreaName(string objectName)
    {
        return objectName == "Room_Start" || objectName.StartsWith("Room_") || objectName.StartsWith("Path_");
    }

    private static bool Contains2D(Bounds bounds, Vector3 position)
    {
        return position.x >= bounds.min.x && position.x <= bounds.max.x && position.y >= bounds.min.y && position.y <= bounds.max.y;
    }

    private static float DistanceToBounds2D(Bounds bounds, Vector3 position)
    {
        float dx = Mathf.Max(bounds.min.x - position.x, 0f, position.x - bounds.max.x);
        float dy = Mathf.Max(bounds.min.y - position.y, 0f, position.y - bounds.max.y);
        return dx * dx + dy * dy;
    }
}
