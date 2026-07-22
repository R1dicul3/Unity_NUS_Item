using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class LevelVariantDemoObject : MonoBehaviour
{
    private readonly Color[] variantColors =
    {
        new Color(0.08f, 0.42f, 1f, 1f),
        new Color(0.95f, 0.1f, 0.12f, 1f)
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

    private void ApplyVariant(bool force)
    {
        if (switcher == null)
        {
            switcher = FindFirstObjectByType<LevelVariantSwitcher>();
        }

        int variant = switcher != null ? Mathf.Clamp(switcher.CurrentIndex, 0, 1) : 0;
        if (!force && variant == appliedVariant)
        {
            return;
        }

        appliedVariant = variant;
        transform.localPosition = new Vector3(-11.2f, -1.2f, 0f);
        spriteRenderer.color = variantColors[variant];
        transform.localScale = new Vector3(3.2f, 0.45f, 1f);
        boxCollider.isTrigger = false;
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
