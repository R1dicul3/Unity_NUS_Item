using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour
{
    public static event System.Action<PlatformerPlayerController, RoomDoor, RoomDoor> PlayerTeleported;

    [SerializeField] private Transform targetSpawn;
    [SerializeField] private Vector2 exitVelocity;
    [SerializeField] private bool preserveHorizontalDirection = true;
    [SerializeField] private bool autoLinkWhenTargetMissing = true;
    [SerializeField] private float alignmentTolerance = 3f;
    [SerializeField] private float maxAutoLinkDistance = 45f;
    [SerializeField] private float exitOffset = 1.25f;
    [SerializeField] private float teleportCooldown = 0.25f;

    private static float nextAllowedTeleportTime;

    private void Reset()
    {
        Collider2D doorCollider = GetComponent<Collider2D>();
        doorCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null || Time.time < nextAllowedTeleportTime)
        {
            return;
        }

        Vector3 destination;
        RoomDoor targetDoor = null;
        if (targetSpawn != null)
        {
            destination = GetTargetSpawnDestination(out targetDoor);
        }
        else if (autoLinkWhenTargetMissing && TryGetAutoLinkedDestination(out destination, out targetDoor))
        {
        }
        else
        {
            return;
        }

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        player.transform.position = destination;
        nextAllowedTeleportTime = Time.time + teleportCooldown;

        if (playerBody != null)
        {
            float xVelocity = preserveHorizontalDirection ? Mathf.Sign(playerBody.linearVelocity.x) * Mathf.Abs(exitVelocity.x) : exitVelocity.x;
            playerBody.linearVelocity = new Vector2(xVelocity, exitVelocity.y);
        }

        PlayerTeleported?.Invoke(player, this, targetDoor);
    }

    public string GetSourceAreaName()
    {
        const string prefix = "Door_";
        const string separator = "_To_";

        string doorName = gameObject.name;
        if (!doorName.StartsWith(prefix))
        {
            return string.Empty;
        }

        int separatorIndex = doorName.IndexOf(separator, System.StringComparison.Ordinal);
        if (separatorIndex <= prefix.Length)
        {
            return string.Empty;
        }

        return doorName.Substring(prefix.Length, separatorIndex - prefix.Length);
    }

    private bool TryGetAutoLinkedDestination(out Vector3 destination, out RoomDoor linkedDoor)
    {
        destination = Vector3.zero;
        linkedDoor = FindNearestAlignedDoor();

        if (linkedDoor == null)
        {
            return false;
        }

        Vector2 travelDirection = linkedDoor.transform.position - transform.position;
        Vector2 exitDirection = GetDominantAxisDirection(travelDirection);
        destination = linkedDoor.transform.position + (Vector3)(exitDirection * exitOffset);
        return true;
    }

    private Vector3 GetTargetSpawnDestination(out RoomDoor targetDoor)
    {
        targetDoor = targetSpawn.GetComponent<RoomDoor>();
        if (targetDoor == null)
        {
            return targetSpawn.position;
        }

        Vector2 travelDirection = targetDoor.transform.position - transform.position;
        Vector2 exitDirection = GetDominantAxisDirection(travelDirection);
        return targetDoor.transform.position + (Vector3)(exitDirection * exitOffset);
    }

    private RoomDoor FindNearestAlignedDoor()
    {
        RoomDoor[] doors = FindObjectsByType<RoomDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        RoomDoor bestDoor = null;
        float bestDistance = float.MaxValue;
        Vector2 currentPosition = transform.position;

        foreach (RoomDoor door in doors)
        {
            if (door == this || !door.isActiveAndEnabled)
            {
                continue;
            }

            Vector2 otherPosition = door.transform.position;
            Vector2 delta = otherPosition - currentPosition;
            float distance = delta.magnitude;
            if (distance <= 0.01f || distance > maxAutoLinkDistance)
            {
                continue;
            }

            bool horizontallyAligned = Mathf.Abs(delta.y) <= alignmentTolerance;
            bool verticallyAligned = Mathf.Abs(delta.x) <= alignmentTolerance;
            if (!horizontallyAligned && !verticallyAligned)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDoor = door;
            }
        }

        return bestDoor;
    }

    private static Vector2 GetDominantAxisDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return Vector2.right;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }
}
