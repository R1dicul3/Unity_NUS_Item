using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingLift2D : MonoBehaviour
{
    [Header("Endpoints")]
    [SerializeField] private float upperLocalY = 98.76f;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float speed = 2f;
    [SerializeField] private bool startMovingUp = true;
    [SerializeField, Min(0.001f)] private float endpointTolerance = 0.01f;

    private Rigidbody2D body;
    private Vector3 lowerLocalPosition;
    private Vector3 upperLocalPosition;
    private bool movingUp;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigureBody();

        lowerLocalPosition = transform.localPosition;
        upperLocalPosition = new Vector3(lowerLocalPosition.x, upperLocalY, lowerLocalPosition.z);

        if (upperLocalPosition.y < lowerLocalPosition.y)
        {
            (lowerLocalPosition, upperLocalPosition) = (upperLocalPosition, lowerLocalPosition);
        }

        movingUp = startMovingUp;
    }

    private void FixedUpdate()
    {
        Vector2 target = GetWorldPosition(movingUp ? upperLocalPosition : lowerLocalPosition);
        Vector2 nextPosition = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);

        if (Vector2.SqrMagnitude(nextPosition - target) <= endpointTolerance * endpointTolerance)
        {
            body.MovePosition(target);
            movingUp = !movingUp;
        }
    }

    private Vector2 GetWorldPosition(Vector3 localPosition)
    {
        Transform parent = transform.parent;
        return parent != null ? parent.TransformPoint(localPosition) : localPosition;
    }

    private void ConfigureBody()
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 lower = Application.isPlaying ? lowerLocalPosition : transform.localPosition;
        Vector3 upper = Application.isPlaying
            ? upperLocalPosition
            : new Vector3(lower.x, upperLocalY, lower.z);

        Transform parent = transform.parent;
        Vector3 lowerWorld = parent != null ? parent.TransformPoint(lower) : lower;
        Vector3 upperWorld = parent != null ? parent.TransformPoint(upper) : upper;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(lowerWorld, upperWorld);
        Gizmos.DrawWireSphere(lowerWorld, 0.12f);
        Gizmos.DrawWireSphere(upperWorld, 0.12f);
    }
}
