using System.Collections.Generic;
using UnityEngine;

public class SinkingPillar2D : MonoBehaviour
{
    public enum SegmentKind
    {
        Empty,
        Yellow,
        Pink,
        Green,
        Gray
    }

    [Header("Layout")]
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private float floorY;
    [SerializeField] private float ceilingY;
    [SerializeField] private float pillarWidth = 2.4f;
    [SerializeField] private float segmentHeight = 4f;
    [SerializeField] private float visualZ = -0.18f;
    [SerializeField] private SegmentKind[] segments = { SegmentKind.Yellow, SegmentKind.Gray, SegmentKind.Pink, SegmentKind.Green };

    [Header("Motion")]
    [SerializeField] private float maxSinkDistance = 12f;
    [SerializeField] private float sinkSpeed = 4f;
    [SerializeField] private float returnSpeed = 7f;
    [SerializeField] private float finalVisibleSegments = 1f;
    [SerializeField] private bool showWrappedPreview = true;
    [SerializeField] private bool keepWrappedPreviewAtTarget;
    [SerializeField, Range(0f, 1f)] private float wrappedPreviewAlpha = 1f;
    [SerializeField] private float standingProbeHeight = 0.22f;
    [SerializeField] private float standingReleaseDelay = 0.12f;

    [Header("Puzzle Link")]
    [SerializeField] private RoomPillarPuzzle2D puzzle;

    [Header("Colors")]
    [SerializeField] private Color yellow = new Color(1f, 0.86f, 0.12f, 1f);
    [SerializeField] private Color pink = new Color(1f, 0.28f, 0.68f, 1f);
    [SerializeField] private Color green = new Color(0.1f, 0.9f, 0.38f, 1f);
    [SerializeField] private Color gray = new Color(0.55f, 0.58f, 0.62f, 1f);

    [Header("Runtime")]
    [SerializeField] private float currentSinkDistance;
    [SerializeField] private float targetSinkDistance;
    [SerializeField] private int targetVisibleSegmentCount;
    [SerializeField] private bool playerStanding;
    [SerializeField] private bool hasBeenActivated;
    [SerializeField] private bool activationArmed = true;

    private readonly List<Transform> segmentObjects = new List<Transform>();
    private readonly List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();
    private readonly List<BoxCollider2D> segmentColliders = new List<BoxCollider2D>();
    private readonly List<SpriteRenderer> wrapRenderers = new List<SpriteRenderer>();
    private readonly List<BoxCollider2D> wrapColliders = new List<BoxCollider2D>();
    private float lastStandingTime = float.NegativeInfinity;
    private static Sprite fallbackSprite;

    public bool HasBeenActivated => hasBeenActivated;

    public bool IsStationary => Mathf.Abs(currentSinkDistance - targetSinkDistance) < 0.001f;

    public string SaveId => name;

    public SaveSystem.PillarState CaptureState()
    {
        return new SaveSystem.PillarState
        {
            pillarId = SaveId,
            currentSinkDistance = currentSinkDistance,
            targetSinkDistance = targetSinkDistance,
            targetVisibleSegmentCount = targetVisibleSegmentCount,
            hasBeenActivated = hasBeenActivated
        };
    }

    public void ApplyState(SaveSystem.PillarState state)
    {
        if (state == null)
        {
            return;
        }

        currentSinkDistance = state.currentSinkDistance;
        targetSinkDistance = state.targetSinkDistance;
        targetVisibleSegmentCount = state.targetVisibleSegmentCount;
        hasBeenActivated = state.hasBeenActivated;
        activationArmed = true;
        UpdateSegmentPositions();
    }

    public void ConfigurePuzzle(RoomPillarPuzzle2D owningPuzzle)
    {
        puzzle = owningPuzzle;
        if (puzzle != null)
        {
            puzzle.RegisterPillar(this);
        }
    }

    public void ConfigureSprite(Sprite sprite)
    {
        blockSprite = sprite;
    }

    public void Configure(float newFloorY, float newCeilingY, float newPillarWidth, float newSegmentHeight, SegmentKind[] newSegments)
    {
        floorY = newFloorY;
        ceilingY = newCeilingY;
        pillarWidth = newPillarWidth;
        segmentHeight = newSegmentHeight;
        segments = CopySegments(newSegments);
        maxSinkDistance = CalculateMaxSinkDistance();
        RefreshTargetState();
    }

    public void ConfigureMotion(float newSinkSpeed, float newReturnSpeed, float newFinalVisibleSegments = 1f)
    {
        sinkSpeed = newSinkSpeed;
        returnSpeed = newReturnSpeed;
        finalVisibleSegments = Mathf.Clamp(newFinalVisibleSegments, 1f, 4f);
        maxSinkDistance = CalculateMaxSinkDistance();
        RefreshTargetState();
    }

    public void ConfigureWrappedPreview(bool value, bool keepAtTarget = false)
    {
        showWrappedPreview = value;
        keepWrappedPreviewAtTarget = value && keepAtTarget;
        if (showWrappedPreview)
        {
            return;
        }

        foreach (SpriteRenderer preview in wrapRenderers)
        {
            if (preview != null)
            {
                preview.enabled = false;
            }
        }
    }

    public IEnumerable<(SegmentKind kind, float worldY)> GetVisibleSegments()
    {
        int visibleIndex = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == SegmentKind.Empty)
            {
                continue;
            }

            if (visibleIndex >= segmentObjects.Count)
            {
                break;
            }

            Transform segment = segmentObjects[visibleIndex];
            float baseY = ceilingY - segmentHeight * (i + 0.5f);
            float mainY = baseY - currentSinkDistance;
            float mainVisibleHeight = GetVisibleHeight(mainY);
            bool mainInRoom = mainVisibleHeight > 0.001f;

            if (mainInRoom)
            {
                yield return (segments[i], segment.position.y);
            }

            visibleIndex++;
        }
    }

    public void ResetPillar()
    {
        hasBeenActivated = false;
        activationArmed = true;
        currentSinkDistance = 0f;
        targetSinkDistance = 0f;
        RefreshTargetState();
        UpdateSegmentPositions();
    }

    public void RebuildConfiguredSegments()
    {
        segments = CopySegments(segments);
        RebuildSegments();
    }

    public void TryNotifyPlayerStanding(Collider2D playerCollider, Collider2D segmentCollider, float topTolerance)
    {
        if (IsPlayerStandingOnSegment(playerCollider, segmentCollider, topTolerance))
        {
            NotifyPlayerStanding();
        }
    }

    private void NotifyPlayerStanding()
    {
        lastStandingTime = Time.time;
    }

    public void SinkToFinalVisibleSegments()
    {
        hasBeenActivated = true;
        int solidCount = CountSolidSegments();
        targetVisibleSegmentCount = Mathf.Clamp(Mathf.RoundToInt(finalVisibleSegments), 1, Mathf.Max(1, solidCount));
        targetSinkDistance = CalculateSinkDistanceForVisibleCount(targetVisibleSegmentCount);
    }

    public void RaiseOneSegment()
    {
        if (!hasBeenActivated)
        {
            return;
        }

        int solidCount = CountSolidSegments();
        targetVisibleSegmentCount = Mathf.Clamp(targetVisibleSegmentCount + 1, 1, Mathf.Max(1, solidCount));
        targetSinkDistance = CalculateSinkDistanceForVisibleCount(targetVisibleSegmentCount);
    }

    private void Awake()
    {
        maxSinkDistance = CalculateMaxSinkDistance();
        RefreshTargetState();
        ResolvePuzzle();
        CacheExistingSegments();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && transform.childCount == 0)
        {
            RebuildSegments();
            return;
        }

        CacheExistingSegments();
    }

    private void Update()
    {
        if (segmentObjects.Count == 0)
        {
            CacheExistingSegments();
        }

        if (Application.isPlaying && IsPlayerStandingOnAnySegment())
        {
            NotifyPlayerStanding();
        }

        bool wasPlayerStanding = playerStanding;
        playerStanding = Application.isPlaying
            && Time.time - lastStandingTime <= Mathf.Max(standingReleaseDelay, Time.fixedDeltaTime * 2f);

        if (!activationArmed && !IsPlayerInsideActivationZone())
        {
            activationArmed = true;
        }

        if (activationArmed && playerStanding && !wasPlayerStanding)
        {
            activationArmed = false;
            ResolvePuzzle();
            if (puzzle != null)
            {
                puzzle.NotifyPillarStepped(this);
            }
            else
            {
                SinkToFinalVisibleSegments();
            }
        }

        bool isSinking = targetSinkDistance > currentSinkDistance;
        float speed = isSinking ? sinkSpeed : returnSpeed;
        currentSinkDistance = Mathf.MoveTowards(currentSinkDistance, targetSinkDistance, speed * Time.deltaTime);
        UpdateSegmentPositions();
    }

    private bool IsPlayerInsideActivationZone()
    {
        Vector3 localCenter = new Vector3(0f, (floorY + ceilingY) * 0.5f, 0f);
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Vector3 scale = transform.lossyScale;
        Vector2 zoneSize = new Vector2(
            Mathf.Max(0.1f, pillarWidth * Mathf.Abs(scale.x)),
            Mathf.Max(0.1f, (ceilingY - floorY) * Mathf.Abs(scale.y) + 1f));

        Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, zoneSize, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.GetComponentInParent<PlatformerPlayerController>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolvePuzzle()
    {
        if (puzzle == null)
        {
            puzzle = GetComponentInParent<RoomPillarPuzzle2D>();
        }

        if (puzzle != null)
        {
            puzzle.RegisterPillar(this);
        }
    }

    private void CacheExistingSegments()
    {
        segmentObjects.Clear();
        segmentRenderers.Clear();
        segmentColliders.Clear();
        wrapRenderers.Clear();
        wrapColliders.Clear();

        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Segment_", System.StringComparison.Ordinal))
            {
                children.Add(child);
            }
        }

        children.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        foreach (Transform child in children)
        {
            segmentObjects.Add(child);

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetBlockSprite();
                renderer.sortingOrder = 6;
            }

            segmentRenderers.Add(renderer);
            SpriteRenderer wrapRenderer = showWrappedPreview ? EnsureWrapRenderer(child, renderer) : null;
            wrapRenderers.Add(wrapRenderer);
            wrapColliders.Add(wrapRenderer != null ? wrapRenderer.GetComponent<BoxCollider2D>() : null);

            BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = child.gameObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
            }

            segmentColliders.Add(collider);

            SinkingPillarSegment2D sensor = child.GetComponent<SinkingPillarSegment2D>();
            if (sensor == null)
            {
                sensor = child.gameObject.AddComponent<SinkingPillarSegment2D>();
            }

            sensor.Initialize(this);
        }
    }

    private void RebuildSegments()
    {
        if (segments == null || segments.Length != 4)
        {
            segments = CopySegments(segments);
        }

        RemoveGeneratedSegments();
        segmentObjects.Clear();
        segmentRenderers.Clear();
        segmentColliders.Clear();
        wrapRenderers.Clear();
        wrapColliders.Clear();

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == SegmentKind.Empty)
            {
                continue;
            }

            GameObject segment = new GameObject($"Segment_{i + 1}_{segments[i]}");
            segment.transform.SetParent(transform, false);
            segmentObjects.Add(segment.transform);

            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBlockSprite();
            renderer.color = GetColor(segments[i]);
            renderer.sortingOrder = 6;
            segmentRenderers.Add(renderer);
            SpriteRenderer wrapRenderer = showWrappedPreview ? EnsureWrapRenderer(segment.transform, renderer) : null;
            wrapRenderers.Add(wrapRenderer);
            wrapColliders.Add(wrapRenderer != null ? wrapRenderer.GetComponent<BoxCollider2D>() : null);

            BoxCollider2D collider = segment.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            segmentColliders.Add(collider);

            SinkingPillarSegment2D sensor = segment.AddComponent<SinkingPillarSegment2D>();
            sensor.Initialize(this);
        }

        UpdateSegmentPositions();
    }

    private void UpdateSegmentPositions()
    {
        int visibleIndex = 0;
        float roomHeight = Mathf.Max(segmentHeight, ceilingY - floorY);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == SegmentKind.Empty)
            {
                continue;
            }

            Transform segment = segmentObjects[visibleIndex];
            float baseY = ceilingY - segmentHeight * (i + 0.5f);
            float mainY = baseY - currentSinkDistance;
            float mainVisibleHeight = GetVisibleHeight(mainY);
            bool mainInRoom = mainVisibleHeight > 0.001f;
            float mainVisibleY = GetVisibleCenterY(mainY);
            segment.localPosition = new Vector3(0f, mainVisibleY, visualZ);
            segment.localScale = new Vector3(pillarWidth, Mathf.Max(mainVisibleHeight, 0.001f), 1f);

            if (visibleIndex < segmentRenderers.Count)
            {
                SpriteRenderer renderer = segmentRenderers[visibleIndex];
                Color color = GetColor(segments[i]);
                color.a = 1f;
                renderer.color = color;
                renderer.enabled = mainInRoom;
            }

            if (visibleIndex < segmentColliders.Count)
            {
                segmentColliders[visibleIndex].enabled = mainInRoom;
            }

            if (visibleIndex < wrapRenderers.Count)
            {
                BoxCollider2D wrapCollider = visibleIndex < wrapColliders.Count ? wrapColliders[visibleIndex] : null;
                UpdateWrapPreview(wrapRenderers[visibleIndex], wrapCollider, segments[i], baseY, roomHeight);
            }

            visibleIndex++;
        }
    }

    private SpriteRenderer EnsureWrapRenderer(Transform segment, SpriteRenderer sourceRenderer)
    {
        string previewName = $"WrapPreview_{segment.name}";
        Transform existing = transform.Find(previewName);
        GameObject previewObject = existing != null ? existing.gameObject : new GameObject("WrapPreview");
        previewObject.name = previewName;
        previewObject.transform.SetParent(transform, false);

        SpriteRenderer preview = previewObject.GetComponent<SpriteRenderer>();
        if (preview == null)
        {
            preview = previewObject.AddComponent<SpriteRenderer>();
        }

        preview.sprite = sourceRenderer != null && sourceRenderer.sprite != null ? sourceRenderer.sprite : GetBlockSprite();
        preview.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder : 6;
        preview.enabled = false;

        BoxCollider2D collider = previewObject.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = previewObject.AddComponent<BoxCollider2D>();
        }

        collider.size = Vector2.one;
        collider.enabled = false;

        SinkingPillarSegment2D sensor = previewObject.GetComponent<SinkingPillarSegment2D>();
        if (sensor == null)
        {
            sensor = previewObject.AddComponent<SinkingPillarSegment2D>();
        }

        sensor.Initialize(this);
        return preview;
    }

    private void UpdateWrapPreview(SpriteRenderer preview, BoxCollider2D collider, SegmentKind kind, float baseY, float roomHeight)
    {
        if (preview == null)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }

            return;
        }

        float wrappedY = baseY + roomHeight - currentSinkDistance;
        float visibleHeight = GetVisibleHeight(wrappedY);
        float remainingDistance = Mathf.Max(0f, maxSinkDistance - currentSinkDistance);
        float completionFade = Mathf.Clamp01(remainingDistance / Mathf.Max(0.01f, segmentHeight * 0.35f));
        bool isSinking = targetSinkDistance > currentSinkDistance + 0.001f;
        bool keepPreviewVisible = keepWrappedPreviewAtTarget
            && hasBeenActivated
            && currentSinkDistance > 0.001f;
        bool showPreview = showWrappedPreview
            && (isSinking || keepPreviewVisible)
            && (keepPreviewVisible || completionFade > 0.001f)
            && visibleHeight > 0.001f;

        preview.enabled = showPreview;
        if (collider != null)
        {
            collider.enabled = showPreview;
        }

        if (!showPreview)
        {
            return;
        }

        Color color = GetColor(kind);
        color.a = wrappedPreviewAlpha * (keepPreviewVisible ? 1f : completionFade);
        preview.color = color;
        preview.transform.localPosition = new Vector3(0f, GetVisibleCenterY(wrappedY), visualZ);
        preview.transform.localScale = new Vector3(pillarWidth, visibleHeight, 1f);
    }

    private float GetVisibleHeight(float centerY)
    {
        float visibleBottom = Mathf.Max(centerY - segmentHeight * 0.5f, floorY);
        float visibleTop = Mathf.Min(centerY + segmentHeight * 0.5f, ceilingY);
        return Mathf.Max(0f, visibleTop - visibleBottom);
    }

    private float GetVisibleCenterY(float centerY)
    {
        float visibleBottom = Mathf.Max(centerY - segmentHeight * 0.5f, floorY);
        float visibleTop = Mathf.Min(centerY + segmentHeight * 0.5f, ceilingY);
        return (visibleBottom + visibleTop) * 0.5f;
    }

    private bool IsPlayerStandingOnAnySegment()
    {
        for (int i = 0; i < segmentColliders.Count; i++)
        {
            if (IsPlayerStandingOnCollider(segmentColliders[i]))
            {
                return true;
            }

            if (i < wrapColliders.Count && IsPlayerStandingOnCollider(wrapColliders[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerStandingOnCollider(Collider2D pillarCollider)
    {
        if (pillarCollider == null || !pillarCollider.enabled)
        {
            return false;
        }

        Bounds bounds = pillarCollider.bounds;
        Vector2 probeCenter = new Vector2(bounds.center.x, bounds.max.y + standingProbeHeight * 0.5f);
        Vector2 probeSize = new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.92f), standingProbeHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == pillarCollider || hit.isTrigger)
            {
                continue;
            }

            if (IsPlayerStandingOnSegment(hit, pillarCollider, standingProbeHeight))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerStandingOnSegment(Collider2D playerCollider, Collider2D segmentCollider, float topTolerance)
    {
        if (playerCollider == null || segmentCollider == null || playerCollider.isTrigger || !segmentCollider.enabled)
        {
            return false;
        }

        PlatformerPlayerController player = playerCollider.GetComponentInParent<PlatformerPlayerController>();
        if (player == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds segmentBounds = segmentCollider.bounds;
        float horizontalOverlap = Mathf.Min(playerBounds.max.x, segmentBounds.max.x)
            - Mathf.Max(playerBounds.min.x, segmentBounds.min.x);
        float requiredOverlap = Mathf.Min(playerBounds.size.x * 0.25f, segmentBounds.size.x * 0.15f);
        float feetOffset = playerBounds.min.y - segmentBounds.max.y;

        Rigidbody2D playerBody = playerCollider.attachedRigidbody;
        bool isNotMovingUp = playerBody == null || playerBody.linearVelocity.y <= 0.25f;
        bool playerIsAbove = playerBounds.center.y > segmentBounds.center.y;
        bool feetAreOnTop = feetOffset >= -Mathf.Max(0.01f, topTolerance)
            && feetOffset <= Mathf.Max(0.05f, standingProbeHeight);

        return isNotMovingUp
            && playerIsAbove
            && feetAreOnTop
            && horizontalOverlap >= requiredOverlap;
    }

    private float CalculateMaxSinkDistance()
    {
        int solidCount = CountSolidSegments();
        if (solidCount == 0)
        {
            return 0f;
        }

        int finalCount = Mathf.Clamp(Mathf.RoundToInt(finalVisibleSegments), 1, solidCount);
        return CalculateSinkDistanceForVisibleCount(finalCount);
    }

    private float CalculateSinkDistanceForVisibleCount(int visibleCount)
    {
        int solidCount = CountSolidSegments();
        if (solidCount == 0 || visibleCount >= solidCount)
        {
            return 0f;
        }

        int clampedVisibleCount = Mathf.Clamp(visibleCount, 1, solidCount);
        int encounteredSolidSegments = 0;
        for (int slotIndex = 0; slotIndex < segments.Length; slotIndex++)
        {
            if (segments[slotIndex] == SegmentKind.Empty)
            {
                continue;
            }

            encounteredSolidSegments++;
            if (encounteredSolidSegments == clampedVisibleCount)
            {
                int bottomSlotIndex = segments.Length - 1;
                return Mathf.Max(0f, (bottomSlotIndex - slotIndex) * segmentHeight);
            }
        }

        return 0f;
    }

    private int CountSolidSegments()
    {
        int count = 0;
        if (segments == null)
        {
            return count;
        }

        foreach (SegmentKind segment in segments)
        {
            if (segment != SegmentKind.Empty)
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshTargetState()
    {
        int solidCount = CountSolidSegments();
        if (solidCount == 0)
        {
            targetVisibleSegmentCount = 0;
            targetSinkDistance = 0f;
            currentSinkDistance = 0f;
            return;
        }

        if (!hasBeenActivated)
        {
            targetVisibleSegmentCount = solidCount;
            targetSinkDistance = 0f;
        }
        else
        {
            targetVisibleSegmentCount = Mathf.Clamp(targetVisibleSegmentCount, 1, solidCount);
            targetSinkDistance = CalculateSinkDistanceForVisibleCount(targetVisibleSegmentCount);
        }

        currentSinkDistance = Mathf.Clamp(currentSinkDistance, 0f, maxSinkDistance);
    }

    private void RemoveGeneratedSegments()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Segment_", System.StringComparison.Ordinal)
                || child.name.StartsWith("WrapPreview_", System.StringComparison.Ordinal))
            {
                children.Add(child.gameObject);
            }
        }

        foreach (GameObject child in children)
        {
            if (Application.isPlaying)
            {
                child.SetActive(false);
                child.name = $"__Removing_{child.name}";
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private SegmentKind[] CopySegments(SegmentKind[] source)
    {
        SegmentKind[] result = new SegmentKind[4];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = source != null && i < source.Length ? source[i] : SegmentKind.Empty;
        }

        return result;
    }

    private Color GetColor(SegmentKind kind)
    {
        switch (kind)
        {
            case SegmentKind.Yellow:
                return yellow;
            case SegmentKind.Pink:
                return pink;
            case SegmentKind.Green:
                return green;
            case SegmentKind.Gray:
                return gray;
            default:
                return Color.clear;
        }
    }

    private Sprite GetBlockSprite()
    {
        return blockSprite != null ? blockSprite : GetFallbackSprite();
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        Texture2D texture = new Texture2D(1, 1)
        {
            name = "Pillar Puzzle Pixel",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "Pillar Puzzle Sprite";
        fallbackSprite.hideFlags = HideFlags.HideAndDontSave;
        return fallbackSprite;
    }
}
