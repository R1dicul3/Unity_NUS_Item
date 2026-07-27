using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CharacterGroundShadow2D : MonoBehaviour {
    [Header("Projection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxProjectionDistance = 8f;
    [SerializeField] private float groundInset = 0.02f;

    [Header("Appearance")]
    [SerializeField] private Vector2 groundedSize = new Vector2(0.8f, 0.18f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private float airborneFadeHeight = 4f;
    [SerializeField] private float minAirborneAlpha = 0.08f;
    [SerializeField] private float minAirborneScale = 0.55f;
    [SerializeField] private int sortingOrderOffset = -1;

    private const string ShadowName = "GroundShadow";
    private BoxCollider2D bodyCollider;
    private SpriteRenderer ownerRenderer;
    private SpriteRenderer shadowRenderer;
    private Sprite generatedSprite;
    private Transform shadowTransform;

    private void Awake() {
        bodyCollider = GetComponent<BoxCollider2D>();
        ownerRenderer = GetComponentInChildren<SpriteRenderer>();
        EnsureShadowRenderer();
    }

    private void LateUpdate() {
        EnsureShadowRenderer();
        UpdateShadow();
    }

    private void OnDisable() {
        if (shadowRenderer != null) {
            shadowRenderer.enabled = false;
        }
    }

    private void OnEnable() {
        if (shadowRenderer != null) {
            shadowRenderer.enabled = true;
        }
    }

    private void OnValidate() {
        groundedSize.x = Mathf.Max(0.01f, groundedSize.x);
        groundedSize.y = Mathf.Max(0.01f, groundedSize.y);
        maxProjectionDistance = Mathf.Max(0.1f, maxProjectionDistance);
        airborneFadeHeight = Mathf.Max(0.1f, airborneFadeHeight);
        minAirborneAlpha = Mathf.Clamp01(minAirborneAlpha);
        minAirborneScale = Mathf.Clamp(minAirborneScale, 0.01f, 1f);
    }

    private void EnsureShadowRenderer() {
        if (shadowRenderer != null) {
            return;
        }

        Transform existing = transform.Find(ShadowName);
        GameObject shadowObject = existing != null ? existing.gameObject : new GameObject(ShadowName);
        shadowTransform = shadowObject.transform;
        shadowTransform.SetParent(transform, false);

        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null) {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        if (generatedSprite == null) {
            generatedSprite = CreateShadowSprite();
        }

        shadowRenderer.sprite = generatedSprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.enabled = isActiveAndEnabled;

        if (ownerRenderer == null) {
            ownerRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (ownerRenderer != null) {
            shadowRenderer.sortingLayerID = ownerRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = ownerRenderer.sortingOrder + sortingOrderOffset;
        }
    }

    private void UpdateShadow() {
        if (bodyCollider == null) {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 feet = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 shadowPosition = feet + Vector2.up * groundInset;
        float heightAboveGround = 0f;

        RaycastHit2D[] hits = Physics2D.RaycastAll(feet + Vector2.up * 0.05f, Vector2.down, maxProjectionDistance, groundMask);
        foreach (RaycastHit2D hit in hits) {
            if (hit.collider == null || IsOwnCollider(hit.collider) || hit.collider.isTrigger) {
                continue;
            }

            shadowPosition = hit.point + Vector2.up * groundInset;
            heightAboveGround = Mathf.Max(0f, feet.y - hit.point.y);
            break;
        }

        float airFactor = Mathf.Clamp01(heightAboveGround / airborneFadeHeight);
        float scaleFactor = Mathf.Lerp(1f, minAirborneScale, airFactor);
        float alpha = Mathf.Lerp(shadowColor.a, minAirborneAlpha, airFactor);

        shadowRenderer.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);
        SetWorldPosition(shadowPosition);
        SetWorldSize(groundedSize * scaleFactor);
    }

    private bool IsOwnCollider(Collider2D candidate) {
        return candidate == bodyCollider || candidate.transform.IsChildOf(transform);
    }

    private void SetWorldPosition(Vector2 worldPosition) {
        Vector3 localPosition = transform.InverseTransformPoint(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
        localPosition.z = 0f;
        shadowTransform.localPosition = localPosition;
    }

    private void SetWorldSize(Vector2 worldSize) {
        Vector3 parentScale = transform.lossyScale;
        shadowTransform.localScale = new Vector3(
            worldSize.x / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
            worldSize.y / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
            1f);
    }

    private static Sprite CreateShadowSprite() {
        const int width = 96;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float nx = (x + 0.5f) / width * 2f - 1f;
                float ny = (y + 0.5f) / height * 2f - 1f;
                float distance = nx * nx + ny * ny;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
    }
}
