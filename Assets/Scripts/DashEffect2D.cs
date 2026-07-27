using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DashEffect2D : MonoBehaviour {
    [Header("Afterimage")]
    [SerializeField] private float afterimageInterval = 0.035f;
    [SerializeField] private float afterimageLifetime = 0.18f;
    [SerializeField] private Color afterimageColor = new Color(0.35f, 0.95f, 1f, 0.42f);
    [SerializeField] private int afterimageSortingOffset = -1;

    [Header("Speed Lines")]
    [SerializeField] private int burstLineCount = 5;
    [SerializeField] private float lineLifetime = 0.14f;
    [SerializeField] private float lineLength = 0.65f;
    [SerializeField] private float lineWidth = 0.045f;
    [SerializeField] private float lineSpread = 0.55f;
    [SerializeField] private Color lineColor = new Color(0.65f, 1f, 1f, 0.75f);
    [SerializeField] private int lineSortingOffset = 1;

    private readonly List<FadingSprite> fadingSprites = new List<FadingSprite>();
    private SpriteRenderer sourceRenderer;
    private Sprite lineSprite;
    private float remainingDashTime;
    private float nextAfterimageTime;
    private float dashDirection = 1f;

    private void Awake() {
        sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        lineSprite = CreateLineSprite();
    }

    private void Update() {
        UpdateDashEmission();
        UpdateFadingSprites();
    }

    public void Play(float direction, float duration) {
        dashDirection = direction >= 0f ? 1f : -1f;
        remainingDashTime = Mathf.Max(0f, duration);
        nextAfterimageTime = 0f;

        SpawnAfterimage();
        SpawnSpeedLines();
    }

    public void Stop() {
        remainingDashTime = 0f;
    }

    private void UpdateDashEmission() {
        if (remainingDashTime <= 0f) {
            return;
        }

        remainingDashTime -= Time.deltaTime;
        nextAfterimageTime -= Time.deltaTime;

        if (nextAfterimageTime <= 0f) {
            SpawnAfterimage();
            nextAfterimageTime = afterimageInterval;
        }
    }

    private void UpdateFadingSprites() {
        for (int i = fadingSprites.Count - 1; i >= 0; i--) {
            FadingSprite sprite = fadingSprites[i];
            sprite.Age += Time.deltaTime;

            if (sprite.Age >= sprite.Lifetime) {
                if (sprite.Renderer != null) {
                    Destroy(sprite.Renderer.gameObject);
                }

                fadingSprites.RemoveAt(i);
                continue;
            }

            float t = sprite.Age / sprite.Lifetime;
            float alpha = Mathf.Lerp(sprite.StartColor.a, 0f, t);
            sprite.Renderer.color = new Color(sprite.StartColor.r, sprite.StartColor.g, sprite.StartColor.b, alpha);
            sprite.Renderer.transform.position = sprite.StartPosition + sprite.Drift * t;
        }
    }

    private void SpawnAfterimage() {
        if (sourceRenderer == null) {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (sourceRenderer == null || sourceRenderer.sprite == null) {
            return;
        }

        GameObject afterimage = new GameObject("DashAfterimage");
        SpriteRenderer renderer = afterimage.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceRenderer.sprite;
        renderer.flipX = sourceRenderer.flipX;
        renderer.flipY = sourceRenderer.flipY;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder + afterimageSortingOffset;
        renderer.color = afterimageColor;

        Transform sourceTransform = sourceRenderer.transform;
        afterimage.transform.position = sourceTransform.position;
        afterimage.transform.rotation = sourceTransform.rotation;
        afterimage.transform.localScale = sourceTransform.lossyScale;

        fadingSprites.Add(new FadingSprite(
            renderer,
            afterimageLifetime,
            afterimageColor,
            afterimage.transform.position,
            new Vector3(-dashDirection * 0.18f, 0f, 0f)));
    }

    private void SpawnSpeedLines() {
        if (sourceRenderer == null) {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        int lineCount = Mathf.Max(0, burstLineCount);
        for (int i = 0; i < lineCount; i++) {
            float verticalOffset = lineCount == 1 ? 0f : Mathf.Lerp(-lineSpread, lineSpread, i / (lineCount - 1f));
            float horizontalOffset = Random.Range(-0.15f, 0.2f);
            Vector3 position = transform.position + new Vector3(-dashDirection * (0.25f + horizontalOffset), verticalOffset, 0f);

            GameObject line = new GameObject("DashSpeedLine");
            SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = lineSprite;
            renderer.color = lineColor;

            if (sourceRenderer != null) {
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                renderer.sortingOrder = sourceRenderer.sortingOrder + lineSortingOffset;
            }

            line.transform.position = position;
            line.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
            line.transform.localScale = new Vector3(lineLength * dashDirection, lineWidth, 1f);

            fadingSprites.Add(new FadingSprite(
                renderer,
                lineLifetime,
                lineColor,
                position,
                new Vector3(-dashDirection * 0.35f, 0f, 0f)));
        }
    }

    private static Sprite CreateLineSprite() {
        Texture2D texture = new Texture2D(8, 1, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int x = 0; x < texture.width; x++) {
            float edgeFade = 1f - Mathf.Abs((x + 0.5f) / texture.width * 2f - 1f);
            texture.SetPixel(x, 0, new Color(1f, 1f, 1f, edgeFade));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }

    private sealed class FadingSprite {
        public readonly SpriteRenderer Renderer;
        public readonly float Lifetime;
        public readonly Color StartColor;
        public readonly Vector3 StartPosition;
        public readonly Vector3 Drift;
        public float Age;

        public FadingSprite(SpriteRenderer renderer, float lifetime, Color startColor, Vector3 startPosition, Vector3 drift) {
            Renderer = renderer;
            Lifetime = Mathf.Max(0.01f, lifetime);
            StartColor = startColor;
            StartPosition = startPosition;
            Drift = drift;
        }
    }
}
