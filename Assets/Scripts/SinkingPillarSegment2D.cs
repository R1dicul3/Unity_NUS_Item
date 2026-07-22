using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SinkingPillarSegment2D : MonoBehaviour
{
    [SerializeField] private SinkingPillar2D pillar;
    [SerializeField] private float topTolerance = 0.22f;

    public void Initialize(SinkingPillar2D owningPillar)
    {
        pillar = owningPillar;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (pillar == null || collision.collider.GetComponentInParent<PlatformerPlayerController>() == null)
        {
            return;
        }

        pillar.TryNotifyPlayerStanding(collision.collider, GetComponent<Collider2D>(), topTolerance);
    }
}
