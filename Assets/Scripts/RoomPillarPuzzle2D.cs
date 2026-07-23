using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomPillarPuzzle2D : MonoBehaviour
{
    [System.Serializable]
    private struct ColorTarget
    {
        public SinkingPillar2D.SegmentKind kind;
        public int pillarIndex;
    }

    [Header("Save")]
    [SerializeField] private string saveId;

    [Header("Room Source")]
    [SerializeField] private Transform roomTransform;
    [SerializeField] private Vector2 fallbackRoomCenter = new Vector2(-27.04f, -18.92f);
    [SerializeField] private Vector2 fallbackRoomSize = new Vector2(72f, 42.3f);
    [SerializeField] private float wallPadding = 1.575f;

    [Header("Pillars")]
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private float pillarWidth = 5.2875f;
    [SerializeField] private float pillarAreaWidthRatio = 0.72f;
    [SerializeField] private float pillarTopPadding = 0.7875f;
    [SerializeField] private float pillarBottomPadding = 0.7875f;
    [SerializeField] private float sinkSpeed = 3.4f;
    [SerializeField] private float returnSpeed = 6.8f;
    [SerializeField] private float finalVisibleSegments = 1f;
    [SerializeField] private float secondPillarFinalVisibleSegments = 2f;

    [Header("Colors")]
    [SerializeField] private Color yellow = new Color(1f, 0.86f, 0.12f, 1f);
    [SerializeField] private Color pink = new Color(1f, 0.28f, 0.68f, 1f);
    [SerializeField] private Color green = new Color(0.1f, 0.9f, 0.38f, 1f);
    [SerializeField] private Color gray = new Color(0.55f, 0.58f, 0.62f, 1f);

    [Header("Completion")]
    [SerializeField] private float alignmentTolerance = 5f;
    [SerializeField] private float targetLineWidth = 3.825f;
    [SerializeField] private float targetLineThickness = 0.27f;
    [SerializeField] private ColorTarget[] colorTargets =
    {
        new ColorTarget { kind = SinkingPillar2D.SegmentKind.Yellow, pillarIndex = 0 },
        new ColorTarget { kind = SinkingPillar2D.SegmentKind.Gray, pillarIndex = 1 },
        new ColorTarget { kind = SinkingPillar2D.SegmentKind.Green, pillarIndex = 2 },
        new ColorTarget { kind = SinkingPillar2D.SegmentKind.Pink, pillarIndex = 3 }
    };

    [Header("Restart")]
    [SerializeField] private string restartPlatformName = "Shared_Platform_01 (2)";

    private readonly List<SinkingPillar2D> registeredPillars = new List<SinkingPillar2D>();
    private static Sprite fallbackSprite;
    private bool isSolved;

    public string SaveId => string.IsNullOrWhiteSpace(saveId) ? GetHierarchyPath(transform) : saveId;

    public void ConfigureSprite(Sprite sprite)
    {
        blockSprite = sprite;
    }

    public void ConfigureRoom(Transform room)
    {
        roomTransform = room;
    }

    public void RegisterPillar(SinkingPillar2D pillar)
    {
        if (pillar != null && !registeredPillars.Contains(pillar))
        {
            registeredPillars.Add(pillar);
        }
    }

    public void NotifyPillarStepped(SinkingPillar2D steppedPillar)
    {
        if (steppedPillar == null)
        {
            return;
        }

        CachePillars();
        foreach (SinkingPillar2D pillar in registeredPillars)
        {
            if (pillar != null && pillar != steppedPillar && pillar.HasBeenActivated)
            {
                pillar.RaiseOneSegment();
            }
        }

        steppedPillar.SinkToFinalVisibleSegments();
    }

    public SaveSystem.PillarPuzzleState CaptureState()
    {
        CachePillars();
        SaveSystem.PillarState[] pillars = new SaveSystem.PillarState[registeredPillars.Count];
        for (int i = 0; i < registeredPillars.Count; i++)
        {
            pillars[i] = registeredPillars[i] != null ? registeredPillars[i].CaptureState() : null;
        }

        return new SaveSystem.PillarPuzzleState
        {
            puzzleId = SaveId,
            pillars = pillars,
            isSolved = isSolved
        };
    }

    public void ApplyState(SaveSystem.PillarPuzzleState state)
    {
        if (state == null || state.pillars == null)
        {
            return;
        }

        CachePillars();
        foreach (SaveSystem.PillarState pillarState in state.pillars)
        {
            if (pillarState == null)
            {
                continue;
            }

            foreach (SinkingPillar2D pillar in registeredPillars)
            {
                if (pillar != null && pillar.SaveId == pillarState.pillarId)
                {
                    pillar.ApplyState(pillarState);
                    break;
                }
            }
        }

        isSolved = state.isSolved;
        SyncPlatformsWithSolvedState();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartPuzzle();
            return;
        }

        if (isSolved)
        {
            return;
        }

        if (IsPlayerOnFailureFloor())
        {
            Debug.Log("Room 1 puzzle failed! Player fell to the floor.");
            RestartPuzzle();
            return;
        }

        if (!AreAllPillarsStationary())
        {
            return;
        }

        if (!HasAnyPillarBeenActivated())
        {
            return;
        }

        if (CheckCompletion())
        {
            isSolved = true;
            Debug.Log("Room 1 puzzle succeeded!");
            ShowSuccessDialog();
        }
        else
        {
            Debug.Log("Room 1 puzzle not solved.");
        }
    }

    private bool AreAllPillarsStationary()
    {
        CachePillars();
        foreach (SinkingPillar2D pillar in registeredPillars)
        {
            if (pillar == null)
            {
                continue;
            }

            if (!pillar.IsStationary)
            {
                return false;
            }
        }

        return registeredPillars.Count > 0;
    }

    private bool HasAnyPillarBeenActivated()
    {
        CachePillars();
        foreach (SinkingPillar2D pillar in registeredPillars)
        {
            if (pillar != null && pillar.HasBeenActivated)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerOnFailureFloor()
    {
        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player == null)
        {
            return false;
        }

        Vector3 pos = player.transform.position;
        return pos.x >= -83.8f && pos.x <= -24.3f && pos.y <= -53f;
    }

    private bool CheckCompletion()
    {
        (string name, Vector3 target)[] checks = new (string, Vector3)[]
        {
            ("Segment_1_Yellow", new Vector3(-63.69f, -35.95f, -0.18f)),
            ("Segment_3_Gray",   new Vector3(-53.07f, -52.47f, -0.18f)),
            ("Segment_2_Green",  new Vector3(-41.99f, -46.82f, -0.18f)),
            ("Segment_2_Pink",   new Vector3(-31.14f, -41.83f, -0.18f))
        };

        const float tolerance = 0.5f;
        bool allPassed = true;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach ((string name, Vector3 target) in checks)
        {
            GameObject segment = GameObject.Find(name);
            if (segment == null)
            {
                sb.AppendLine($"  {name}: NOT FOUND");
                allPassed = false;
                continue;
            }

            float dist = Vector3.Distance(segment.transform.position, target);
            bool pass = dist <= tolerance;
            sb.AppendLine($"  {name}: pos={segment.transform.position}, dist={dist:F3}, pass={pass}");
            if (!pass)
            {
                allPassed = false;
            }
        }

        Debug.Log($"Room 1 Puzzle Check (result={allPassed}):\n{sb}");
        return allPassed;
    }

    public void RestartPuzzle()
    {
        isSolved = false;
        HidePlatforms();

        CachePillars();
        foreach (SinkingPillar2D pillar in registeredPillars)
        {
            if (pillar != null)
            {
                pillar.ResetPillar();
            }
        }

        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player == null)
        {
            Debug.LogWarning("Player not found for restart teleport.");
            return;
        }

        player.transform.position = new Vector3(-53.8f, -28.9f, 0f);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Room 1 puzzle reset, player teleported.");
    }

    private void RevealPlatformsAndTeleportPlayer()
    {
        GameObject platformsParent = GameObject.Find("Shared_Platforms");
        if (platformsParent != null)
        {
            SharedPlatformsReveal reveal = platformsParent.GetComponent<SharedPlatformsReveal>();
            if (reveal == null)
            {
                reveal = platformsParent.AddComponent<SharedPlatformsReveal>();
            }

            reveal.Reveal();
        }

        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player == null)
        {
            Debug.LogWarning("Player not found for teleport.");
            return;
        }

        player.transform.position = new Vector3(-75.9f, -10.5f, 0f);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    private void HidePlatforms()
    {
        GameObject platformsParent = GameObject.Find("Shared_Platforms");
        if (platformsParent != null)
        {
            SharedPlatformsReveal reveal = platformsParent.GetComponent<SharedPlatformsReveal>();
            if (reveal != null)
            {
                reveal.HideInstant();
            }
        }
    }

    private void EnsurePlatformsHidden()
    {
        GameObject platformsParent = GameObject.Find("Shared_Platforms");
        if (platformsParent == null)
        {
            return;
        }

        SharedPlatformsReveal reveal = platformsParent.GetComponent<SharedPlatformsReveal>();
        if (reveal == null)
        {
            reveal = platformsParent.AddComponent<SharedPlatformsReveal>();
        }

        reveal.HideInstant();
    }

    private void SyncPlatformsWithSolvedState()
    {
        GameObject platformsParent = GameObject.Find("Shared_Platforms");
        if (platformsParent == null)
        {
            return;
        }

        SharedPlatformsReveal reveal = platformsParent.GetComponent<SharedPlatformsReveal>();
        if (reveal == null)
        {
            reveal = platformsParent.AddComponent<SharedPlatformsReveal>();
        }

        if (isSolved)
        {
            reveal.SetRevealedInstant();
        }
        else
        {
            reveal.HideInstant();
        }
    }

    private void ShowSuccessDialog()
    {
        Time.timeScale = 0f;
        MainMenu.ConfirmDialogUI.Show(
            "Room 1 puzzle solved!",
            onConfirm: () =>
            {
                Time.timeScale = 1f;
                RevealPlatformsAndTeleportPlayer();
            },
            onCancel: () =>
            {
                Time.timeScale = 1f;
                RevealPlatformsAndTeleportPlayer();
            },
            dialogSound: SoundType.UISuccess);
    }

    private void Awake()
    {
        EnsureSaveId();
        EnsureColorTargets();
        EnsureGeneratedPuzzleContent();
        CachePillars();
        EnsurePlatformsHidden();
    }

    private void OnValidate()
    {
        EnsureSaveId();
        EnsureColorTargets();
    }

    private void OnEnable()
    {
        EnsureGeneratedPuzzleContent();
    }

    [ContextMenu("Rebuild Puzzle")]
    public void RebuildPuzzle()
    {
        ResolveRoomTransform();
        EnsureColorTargets();
        registeredPillars.Clear();
        ClearGeneratedChildren();

        Bounds roomBounds = GetRoomBoundsInPuzzleSpace();
        float floorY = roomBounds.min.y + wallPadding + pillarBottomPadding;
        float ceilingY = roomBounds.max.y - wallPadding - pillarTopPadding;
        float segmentHeight = (ceilingY - floorY) / 4f;
        float usableWidth = (roomBounds.size.x - wallPadding * 2f) * Mathf.Clamp01(pillarAreaWidthRatio);
        float innerLeft = roomBounds.center.x - usableWidth * 0.5f + pillarWidth * 0.5f;
        float innerRight = roomBounds.center.x + usableWidth * 0.5f - pillarWidth * 0.5f;
        float spacing = (innerRight - innerLeft) / 3f;
        float targetY = floorY + segmentHeight * 0.5f;

        CreatePillar("Pillar_01", innerLeft + spacing * 0f, floorY, ceilingY, segmentHeight, finalVisibleSegments, false, false, new[]
        {
            SinkingPillar2D.SegmentKind.Yellow,
            SinkingPillar2D.SegmentKind.Gray,
            SinkingPillar2D.SegmentKind.Pink,
            SinkingPillar2D.SegmentKind.Green
        });

        CreatePillar("Pillar_02", innerLeft + spacing * 1f, floorY, ceilingY, segmentHeight, secondPillarFinalVisibleSegments, true, true, new[]
        {
            SinkingPillar2D.SegmentKind.Green,
            SinkingPillar2D.SegmentKind.Empty,
            SinkingPillar2D.SegmentKind.Gray,
            SinkingPillar2D.SegmentKind.Pink
        });

        CreatePillar("Pillar_03", innerLeft + spacing * 2f, floorY, ceilingY, segmentHeight, finalVisibleSegments, true, true, new[]
        {
            SinkingPillar2D.SegmentKind.Empty,
            SinkingPillar2D.SegmentKind.Green,
            SinkingPillar2D.SegmentKind.Pink,
            SinkingPillar2D.SegmentKind.Gray
        });

        CreatePillar("Pillar_04", innerLeft + spacing * 3f, floorY, ceilingY, segmentHeight, finalVisibleSegments, true, true, new[]
        {
            SinkingPillar2D.SegmentKind.Empty,
            SinkingPillar2D.SegmentKind.Pink,
            SinkingPillar2D.SegmentKind.Yellow,
            SinkingPillar2D.SegmentKind.Green
        });

        CreateBackgroundColorLines(targetY, innerLeft, spacing);
    }

    private void CreatePillar(
        string name,
        float x,
        float floorY,
        float ceilingY,
        float segmentHeight,
        float targetVisibleSegments,
        bool showWrappedPreview,
        bool keepWrappedPreviewAtTarget,
        SinkingPillar2D.SegmentKind[] segments)
    {
        GameObject pillar = new GameObject(name);
        pillar.transform.SetParent(transform, false);
        pillar.transform.localPosition = new Vector3(x, 0f, 0f);

        SinkingPillar2D controller = pillar.AddComponent<SinkingPillar2D>();
        controller.ConfigurePuzzle(this);
        controller.ConfigureSprite(GetBlockSprite());
        controller.Configure(floorY, ceilingY, pillarWidth, segmentHeight, segments);
        controller.ConfigureMotion(sinkSpeed, returnSpeed, targetVisibleSegments);
        controller.ConfigureWrappedPreview(showWrappedPreview, keepWrappedPreviewAtTarget);
        controller.RebuildConfiguredSegments();
    }

    private void CachePillars()
    {
        SinkingPillar2D[] pillars = GetComponentsInChildren<SinkingPillar2D>(true);
        foreach (SinkingPillar2D pillar in pillars)
        {
            if (pillar.name.StartsWith("__Removing_", System.StringComparison.Ordinal))
            {
                continue;
            }

            RegisterPillar(pillar);
            pillar.ConfigurePuzzle(this);
        }

        registeredPillars.RemoveAll(pillar => pillar == null);
    }

    private void EnsureGeneratedPuzzleContent()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveRoomTransform();
        EnsureColorTargets();

        if (!HasGeneratedPillars())
        {
            RebuildPuzzle();
            return;
        }

        if (transform.Find("Background_Color_Lines") == null)
        {
            CreateBackgroundColorLines();
        }
    }

    private bool HasGeneratedPillars()
    {
        foreach (SinkingPillar2D pillar in GetComponentsInChildren<SinkingPillar2D>(true))
        {
            if (pillar != null && !pillar.name.StartsWith("__Removing_", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveRoomTransform()
    {
        if (roomTransform != null && roomTransform != transform)
        {
            return;
        }

        GameObject room = GameObject.Find("Room_1");
        if (room != null && room.transform != transform)
        {
            roomTransform = room.transform;
            return;
        }

        roomTransform = null;
    }

    private Bounds GetRoomBoundsInPuzzleSpace()
    {
        Bounds worldBounds;
        if (roomTransform == null || roomTransform == transform)
        {
            worldBounds = new Bounds(fallbackRoomCenter, fallbackRoomSize);
        }
        else
        {
            worldBounds = new Bounds(
                roomTransform.position,
                new Vector3(Mathf.Abs(roomTransform.lossyScale.x), Mathf.Abs(roomTransform.lossyScale.y), 1f));
        }

        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Bounds localBounds = new Bounds(transform.InverseTransformPoint(new Vector3(min.x, min.y, 0f)), Vector3.zero);
        localBounds.Encapsulate(transform.InverseTransformPoint(new Vector3(min.x, max.y, 0f)));
        localBounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, min.y, 0f)));
        localBounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, max.y, 0f)));
        localBounds.size = new Vector3(localBounds.size.x, localBounds.size.y, 1f);
        return localBounds;
    }

    private float GetTargetLineWorldY()
    {
        Bounds roomBounds = GetRoomBoundsInPuzzleSpace();
        float floorY = roomBounds.min.y + wallPadding + pillarBottomPadding;
        float ceilingY = roomBounds.max.y - wallPadding - pillarTopPadding;
        float segmentHeight = (ceilingY - floorY) / 4f;
        return transform.TransformPoint(new Vector3(0f, floorY + segmentHeight * 0.5f, 0f)).y;
    }

    private void GetTargetLineLayout(out float targetY, out float innerLeft, out float spacing)
    {
        Bounds roomBounds = GetRoomBoundsInPuzzleSpace();
        float floorY = roomBounds.min.y + wallPadding + pillarBottomPadding;
        float ceilingY = roomBounds.max.y - wallPadding - pillarTopPadding;
        float segmentHeight = (ceilingY - floorY) / 4f;
        float usableWidth = (roomBounds.size.x - wallPadding * 2f) * Mathf.Clamp01(pillarAreaWidthRatio);
        innerLeft = roomBounds.center.x - usableWidth * 0.5f + pillarWidth * 0.5f;
        float innerRight = roomBounds.center.x + usableWidth * 0.5f - pillarWidth * 0.5f;
        spacing = (innerRight - innerLeft) / 3f;
        targetY = floorY + segmentHeight * 0.5f;
    }

    private IEnumerable<SinkingPillar2D.SegmentKind> GetRequiredTargetColors()
    {
        EnsureColorTargets();
        HashSet<SinkingPillar2D.SegmentKind> colors = new HashSet<SinkingPillar2D.SegmentKind>();
        foreach (ColorTarget target in colorTargets)
        {
            if (target.kind != SinkingPillar2D.SegmentKind.Empty && colors.Add(target.kind))
            {
                yield return target.kind;
            }
        }
    }

    private void CreateBackgroundColorLines(float y, float innerLeft, float spacing)
    {
        GameObject linesRoot = new GameObject("Background_Color_Lines");
        linesRoot.transform.SetParent(transform, false);
        linesRoot.transform.localPosition = Vector3.zero;

        foreach (ColorTarget target in colorTargets)
        {
            int pillarIndex = Mathf.Clamp(target.pillarIndex, 0, 3);
            GameObject line = new GameObject($"Target_{target.kind}_{pillarIndex + 1}");
            line.transform.SetParent(linesRoot.transform, false);
            line.transform.localPosition = new Vector3(innerLeft + spacing * pillarIndex, y, 0.05f);
            line.transform.localScale = new Vector3(Mathf.Max(0.1f, targetLineWidth), Mathf.Max(0.01f, targetLineThickness), 1f);

            SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBlockSprite();
            renderer.color = GetColor(target.kind);
            renderer.sortingOrder = 5;
        }
    }

    private void CreateBackgroundColorLines()
    {
        GetTargetLineLayout(out float targetY, out float innerLeft, out float spacing);
        CreateBackgroundColorLines(targetY, innerLeft, spacing);
    }

    private void ClearGeneratedChildren()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name == "Background_Color_Lines"
                || child.name.StartsWith("Pillar_", System.StringComparison.Ordinal))
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

    private Sprite GetBlockSprite()
    {
        return blockSprite != null ? blockSprite : GetFallbackSprite();
    }

    private Color GetColor(SinkingPillar2D.SegmentKind kind)
    {
        switch (kind)
        {
            case SinkingPillar2D.SegmentKind.Yellow:
                return yellow;
            case SinkingPillar2D.SegmentKind.Pink:
                return pink;
            case SinkingPillar2D.SegmentKind.Green:
                return green;
            case SinkingPillar2D.SegmentKind.Gray:
                return gray;
            default:
                return Color.clear;
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        Texture2D texture = new Texture2D(1, 1)
        {
            name = "Pillar Puzzle Line Pixel",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "Pillar Puzzle Line Sprite";
        fallbackSprite.hideFlags = HideFlags.HideAndDontSave;
        return fallbackSprite;
    }

    private void EnsureSaveId()
    {
        if (string.IsNullOrWhiteSpace(saveId))
        {
            saveId = GetHierarchyPath(transform);
        }
    }

    private void EnsureColorTargets()
    {
        if (colorTargets != null && colorTargets.Length > 0)
        {
            return;
        }

        colorTargets = new[]
        {
            new ColorTarget { kind = SinkingPillar2D.SegmentKind.Yellow, pillarIndex = 0 },
            new ColorTarget { kind = SinkingPillar2D.SegmentKind.Gray, pillarIndex = 1 },
            new ColorTarget { kind = SinkingPillar2D.SegmentKind.Green, pillarIndex = 2 },
            new ColorTarget { kind = SinkingPillar2D.SegmentKind.Pink, pillarIndex = 3 }
        };
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return string.Empty;
        }

        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
