using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomPillarPuzzle2D : MonoBehaviour
{
    [Header("Save")]
    [SerializeField] private string saveId;

    [Header("Room Source")]
    [SerializeField] private Transform roomTransform;
    [SerializeField] private Vector2 fallbackRoomCenter = new Vector2(-27.04f, -18.92f);
    [SerializeField] private Vector2 fallbackRoomSize = new Vector2(32f, 18.8f);
    [SerializeField] private float wallPadding = 0.7f;

    [Header("Pillars")]
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private float pillarWidth = 2.35f;
    [SerializeField] private float pillarAreaWidthRatio = 0.72f;
    [SerializeField] private float pillarTopPadding = 0.35f;
    [SerializeField] private float pillarBottomPadding = 0.35f;
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
    [SerializeField] private float alignmentTolerance = 0.5f;

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

    private bool CheckCompletion()
    {
        Transform linesParent = transform.Find("Background_Color_Lines");
        if (linesParent == null)
        {
            return false;
        }

        Dictionary<SinkingPillar2D.SegmentKind, float> lineYByColor = new Dictionary<SinkingPillar2D.SegmentKind, float>();
        foreach (Transform line in linesParent)
        {
            SinkingPillar2D.SegmentKind? kind = ParseLineColor(line.name);
            if (kind.HasValue)
            {
                lineYByColor[kind.Value] = line.position.y;
            }
        }

        SinkingPillar2D.SegmentKind[] requiredColors =
        {
            SinkingPillar2D.SegmentKind.Yellow,
            SinkingPillar2D.SegmentKind.Pink,
            SinkingPillar2D.SegmentKind.Green,
            SinkingPillar2D.SegmentKind.Gray
        };

        foreach (SinkingPillar2D.SegmentKind color in requiredColors)
        {
            if (!lineYByColor.TryGetValue(color, out float lineY))
            {
                return false;
            }

            bool found = false;
            foreach (SinkingPillar2D pillar in registeredPillars)
            {
                if (pillar == null)
                {
                    continue;
                }

                foreach ((SinkingPillar2D.SegmentKind kind, float worldY) in pillar.GetVisibleSegments())
                {
                    if (kind == color && Mathf.Abs(worldY - lineY) <= alignmentTolerance)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static SinkingPillar2D.SegmentKind? ParseLineColor(string lineName)
    {
        if (lineName.Contains("Yellow"))
        {
            return SinkingPillar2D.SegmentKind.Yellow;
        }

        if (lineName.Contains("Pink"))
        {
            return SinkingPillar2D.SegmentKind.Pink;
        }

        if (lineName.Contains("Green"))
        {
            return SinkingPillar2D.SegmentKind.Green;
        }

        if (lineName.Contains("Gray"))
        {
            return SinkingPillar2D.SegmentKind.Gray;
        }

        return null;
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

        GameObject platform = GameObject.Find(restartPlatformName);
        if (platform == null)
        {
            Debug.LogWarning($"Restart platform '{restartPlatformName}' not found.");
            return;
        }

        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player == null)
        {
            Debug.LogWarning("Player not found for restart teleport.");
            return;
        }

        Vector3 platformPos = platform.transform.position;
        Vector3 platformScale = platform.transform.lossyScale;
        float platformTop = platformPos.y + platformScale.y * 0.5f;

        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        float playerHeight = playerCollider != null ? playerCollider.size.y * player.transform.lossyScale.y : 1f;
        float teleportY = platformTop + playerHeight * 0.5f + 0.05f;

        player.transform.position = new Vector3(platformPos.x, teleportY, player.transform.position.z);

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

        GameObject targetPlatform = GameObject.Find("Shared_Platform_01 (2)");
        if (targetPlatform == null)
        {
            Debug.LogWarning("Target platform 'Shared_Platform_01 (2)' not found for teleport.");
            return;
        }

        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player == null)
        {
            Debug.LogWarning("Player not found for teleport.");
            return;
        }

        Vector3 platformPos = targetPlatform.transform.position;
        Vector3 platformScale = targetPlatform.transform.lossyScale;
        float platformTop = platformPos.y + platformScale.y * 0.5f;

        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        float playerHeight = playerCollider != null ? playerCollider.size.y * player.transform.lossyScale.y : 1f;
        float teleportY = platformTop + playerHeight * 0.5f + 0.05f;

        player.transform.position = new Vector3(platformPos.x, teleportY, player.transform.position.z);

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
            });
    }

    private void Awake()
    {
        EnsureSaveId();
        CachePillars();
        EnsurePlatformsHidden();
    }

    private void OnValidate()
    {
        EnsureSaveId();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && transform.childCount == 0)
        {
            RebuildPuzzle();
        }
    }

    [ContextMenu("Rebuild Puzzle")]
    public void RebuildPuzzle()
    {
        ResolveRoomTransform();
        registeredPillars.Clear();
        ClearGeneratedChildren();

        Bounds roomBounds = GetRoomBoundsInLayoutSpace();
        float floorY = roomBounds.min.y + wallPadding + pillarBottomPadding;
        float ceilingY = roomBounds.max.y - wallPadding - pillarTopPadding;
        float segmentHeight = (ceilingY - floorY) / 4f;
        float usableWidth = (roomBounds.size.x - wallPadding * 2f) * Mathf.Clamp01(pillarAreaWidthRatio);
        float innerLeft = roomBounds.center.x - usableWidth * 0.5f + pillarWidth * 0.5f;
        float innerRight = roomBounds.center.x + usableWidth * 0.5f - pillarWidth * 0.5f;
        float spacing = (innerRight - innerLeft) / 3f;

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
            RegisterPillar(pillar);
            pillar.ConfigurePuzzle(this);
        }

        registeredPillars.RemoveAll(pillar => pillar == null);
    }

    private void ResolveRoomTransform()
    {
        if (roomTransform != null)
        {
            return;
        }

        GameObject room = GameObject.Find("Room_1");
        if (room != null)
        {
            roomTransform = room.transform;
        }
    }

    private Bounds GetRoomBoundsInLayoutSpace()
    {
        if (roomTransform == null)
        {
            return new Bounds(fallbackRoomCenter, fallbackRoomSize);
        }

        Bounds worldBounds = new Bounds(
            roomTransform.position,
            new Vector3(Mathf.Abs(roomTransform.lossyScale.x), Mathf.Abs(roomTransform.lossyScale.y), 1f));

        Transform layoutSpace = transform.parent;
        if (layoutSpace == null)
        {
            return worldBounds;
        }

        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Bounds localBounds = new Bounds(layoutSpace.InverseTransformPoint(new Vector3(min.x, min.y, 0f)), Vector3.zero);
        localBounds.Encapsulate(layoutSpace.InverseTransformPoint(new Vector3(min.x, max.y, 0f)));
        localBounds.Encapsulate(layoutSpace.InverseTransformPoint(new Vector3(max.x, min.y, 0f)));
        localBounds.Encapsulate(layoutSpace.InverseTransformPoint(new Vector3(max.x, max.y, 0f)));
        localBounds.size = new Vector3(localBounds.size.x, localBounds.size.y, 1f);
        return localBounds;
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
