using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GoalFlag : MonoBehaviour
{
    private BoxCollider2D triggerCollider;
    private bool triggered = false;

    public System.Action onTriggered;

    public void Initialize(Vector2 position, System.Action callback)
    {
        transform.position = position;
        onTriggered = callback;

        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1f, 1.5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
        {
            return;
        }

        if (other.GetComponentInParent<PlatformerPlayerController>() != null)
        {
            triggered = true;
            onTriggered?.Invoke();
        }
    }
}
