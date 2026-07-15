using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour
{
    [SerializeField] private Transform targetSpawn;
    [SerializeField] private Vector2 exitVelocity;
    [SerializeField] private bool preserveHorizontalDirection = true;

    private void Reset()
    {
        Collider2D doorCollider = GetComponent<Collider2D>();
        doorCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null || targetSpawn == null)
        {
            return;
        }

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        player.transform.position = targetSpawn.position;

        if (playerBody != null)
        {
            float xVelocity = preserveHorizontalDirection ? Mathf.Sign(playerBody.linearVelocity.x) * Mathf.Abs(exitVelocity.x) : exitVelocity.x;
            playerBody.linearVelocity = new Vector2(xVelocity, exitVelocity.y);
        }
    }
}
