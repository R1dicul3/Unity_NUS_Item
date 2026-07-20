using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour {
    public float amplitude = 0.4f;
    public float frequency = 3f;

    public float normalFriction = 0.4f;
    public float slipperyFriction = 0f;

    Vector3 startPos;
    Collider2D platformCollider;
    PhysicsMaterial2D platformMaterial;

    void Start() {
        startPos = transform.position;

        platformCollider = GetComponent<Collider2D>();
        platformMaterial = new PhysicsMaterial2D("Platform Material");
        platformMaterial.bounciness = 0f;
        platformMaterial.friction = normalFriction;
        platformCollider.sharedMaterial = platformMaterial;
    }

    void Update() {
        switch (EmotionManager.Instance.CurrentEmotion) {
            case EmotionType.Angry:
                Shake();
                platformMaterial.friction = normalFriction;
                break;

            case EmotionType.Sad:
                transform.position = startPos;
                platformMaterial.friction = slipperyFriction;
                break;

            default: // Calm
                transform.position = startPos;
                platformMaterial.friction = normalFriction;
                break;
        }
    }

    void Shake() {
        float y = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = startPos + Vector3.up * y;
    }
}
