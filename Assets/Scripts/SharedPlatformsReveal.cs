using UnityEngine;

public class SharedPlatformsReveal : MonoBehaviour
{
    [Header("Reveal")]
    [SerializeField] private float hiddenOffsetX = -5f;
    [SerializeField] private float revealSpeed = 3f;

    private bool isRevealed;
    private bool isRevealing;

    private void Update()
    {
        if (!isRevealing)
        {
            return;
        }

        Vector3 pos = transform.localPosition;
        pos.x = Mathf.MoveTowards(pos.x, 0f, revealSpeed * Time.deltaTime);
        transform.localPosition = pos;

        if (Mathf.Approximately(pos.x, 0f))
        {
            isRevealing = false;
            isRevealed = true;
        }
    }

    public void Reveal()
    {
        if (isRevealed || isRevealing)
        {
            return;
        }

        isRevealing = true;
    }

    public void HideInstant()
    {
        isRevealed = false;
        isRevealing = false;
        Vector3 pos = transform.localPosition;
        pos.x = hiddenOffsetX;
        transform.localPosition = pos;
    }

    public void SetRevealedInstant()
    {
        isRevealed = true;
        isRevealing = false;
        Vector3 pos = transform.localPosition;
        pos.x = 0f;
        transform.localPosition = pos;
    }
}
