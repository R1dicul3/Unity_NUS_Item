using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(2f, 1.2f);
    [SerializeField] private float smoothTime = 0.16f;
    [SerializeField] private float minY = -1.5f;

    private Vector3 velocity;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x + offset.x, Mathf.Max(target.position.y + offset.y, minY), transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
