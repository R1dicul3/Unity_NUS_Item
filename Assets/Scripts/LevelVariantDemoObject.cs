using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class LevelVariantDemoObject : MonoBehaviour
{
    private static readonly Vector3[] VariantPositions =
    {
        new Vector3(-11.2f, -1.2f, 0f),
        new Vector3(-8.3f, -2.3f, 0f),
        new Vector3(-5.2f, -1.2f, 0f)
    };

    private readonly Color[] variantColors =
    {
        new Color(0.18f, 0.55f, 1f, 1f),
        new Color(1f, 0.82f, 0.16f, 1f),
        new Color(0.16f, 0.88f, 0.48f, 1f)
    };

    private LevelVariantSwitcher switcher;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private int appliedVariant = -1;

    public static LevelVariantDemoObject Create(Transform parent, LevelVariantSwitcher variantSwitcher)
    {
        GameObject demoObject = new GameObject("StartRoom_Variant_Demo_Object");
        demoObject.transform.SetParent(parent, false);
        demoObject.layer = LayerMask.NameToLayer("Default");

        SpriteRenderer renderer = demoObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.sortingOrder = 5;

        BoxCollider2D collider = demoObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        LevelVariantDemoObject demo = demoObject.AddComponent<LevelVariantDemoObject>();
        demo.Initialize(variantSwitcher);
        return demo;
    }

    public void Initialize(LevelVariantSwitcher variantSwitcher)
    {
        switcher = variantSwitcher;
        CacheComponents();
        ApplyVariant(force: true);
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void Update()
    {
        ApplyVariant(force: false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (appliedVariant != 0)
        {
            return;
        }

        PlatformerPlayerController player = collision.collider.GetComponentInParent<PlatformerPlayerController>();
        if (player == null)
        {
            return;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null && body.linearVelocity.y < 0f)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(body.linearVelocity.y, -2f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();
        if (player == null)
        {
            return;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            return;
        }

        if (appliedVariant == 1)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, 16f);
        }
        else if (appliedVariant == 2)
        {
            body.position += new Vector2(4f, 1.5f);
            body.linearVelocity = new Vector2(8f, 6f);
        }
    }

    private void ApplyVariant(bool force)
    {
        if (switcher == null)
        {
            switcher = FindFirstObjectByType<LevelVariantSwitcher>();
        }

        int variant = switcher != null ? Mathf.Clamp(switcher.CurrentIndex, 0, 2) : 0;
        if (!force && variant == appliedVariant)
        {
            return;
        }

        appliedVariant = variant;
        transform.localPosition = VariantPositions[variant];
        spriteRenderer.color = variantColors[variant];

        if (variant == 0)
        {
            transform.localScale = new Vector3(3.2f, 0.45f, 1f);
            boxCollider.isTrigger = false;
        }
        else if (variant == 1)
        {
            transform.localScale = new Vector3(2.4f, 0.35f, 1f);
            boxCollider.isTrigger = true;
        }
        else
        {
            transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            boxCollider.isTrigger = true;
        }
    }

    private void CacheComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
