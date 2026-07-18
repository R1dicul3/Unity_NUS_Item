using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WhiteboxDoorAutoBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string WhiteboxRootName = "White_Box";
    private const float EdgeTolerance = 4f;
    private const float MinimumDoorSpan = 2.5f;
    private const float DoorInset = 0.6f;
    private const float DoorThickness = 1f;
    private const float DoorLength = 3f;
    private const float ExitOffset = 1.25f;

    private sealed class WhiteboxArea
    {
        public Transform Transform;
        public Bounds Bounds;
    }

    private sealed class DoorConnection
    {
        public WhiteboxArea A;
        public WhiteboxArea B;
        public Vector2 Center;
        public Vector2 NormalFromAToB;
        public bool VerticalDoor;
        public float Span;
    }

    [MenuItem("Tools/Whitebox/Rebuild Connected Room Doors")]
    public static void RebuildCurrentSceneDoors()
    {
        RebuildDoorsInScene();
    }

    public static void RebuildSceneDoors()
    {
        EditorSceneManager.OpenScene(ScenePath);
        RebuildDoorsInScene();
        EditorSceneManager.SaveOpenScenes();
    }

    private static void RebuildDoorsInScene()
    {
        GameObject whiteboxRoot = GameObject.Find(WhiteboxRootName);
        if (whiteboxRoot == null)
        {
            Debug.LogWarning($"Whitebox door build skipped: missing {WhiteboxRootName}.");
            return;
        }

        List<WhiteboxArea> areas = CollectAreas(whiteboxRoot.transform);
        RemoveExistingDoors(whiteboxRoot.transform);

        Sprite doorSprite = FindReferenceSprite(areas);
        List<DoorConnection> connections = FindConnections(areas);
        int doorCount = 0;

        foreach (DoorConnection connection in connections)
        {
            RoomDoor doorA = CreateDoor(whiteboxRoot.transform, connection.A, connection.B, connection.Center, -connection.NormalFromAToB, connection.VerticalDoor, doorSprite);
            RoomDoor doorB = CreateDoor(whiteboxRoot.transform, connection.B, connection.A, connection.Center, connection.NormalFromAToB, connection.VerticalDoor, doorSprite);
            LinkDoors(doorA, doorB);
            LinkDoors(doorB, doorA);
            doorCount += 2;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Whitebox door build complete: {connections.Count} connections, {doorCount} doors.");
    }

    private static List<WhiteboxArea> CollectAreas(Transform root)
    {
        List<WhiteboxArea> areas = new List<WhiteboxArea>();

        foreach (Transform child in root)
        {
            if (!IsWhiteboxAreaName(child.name) || !TryGetSolidBounds(child, out Bounds bounds))
            {
                continue;
            }

            areas.Add(new WhiteboxArea
            {
                Transform = child,
                Bounds = bounds
            });
        }

        return areas;
    }

    private static bool IsWhiteboxAreaName(string objectName)
    {
        return objectName == "Room_Start"
            || objectName.StartsWith("Room_")
            || objectName.StartsWith("Path_");
    }

    private static bool TryGetSolidBounds(Transform area, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        BoxCollider2D[] colliders = area.GetComponentsInChildren<BoxCollider2D>(true);

        foreach (BoxCollider2D collider in colliders)
        {
            if (collider.isTrigger || collider.GetComponent<RoomDoor>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static void RemoveExistingDoors(Transform root)
    {
        RoomDoor[] rootDoors = root.GetComponentsInChildren<RoomDoor>(true);
        foreach (RoomDoor door in rootDoors)
        {
            Object.DestroyImmediate(door.gameObject);
        }
    }

    private static Sprite FindReferenceSprite(List<WhiteboxArea> areas)
    {
        foreach (WhiteboxArea area in areas)
        {
            SpriteRenderer renderer = area.Transform.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return null;
    }

    private static List<DoorConnection> FindConnections(List<WhiteboxArea> areas)
    {
        List<DoorConnection> connections = new List<DoorConnection>();

        for (int i = 0; i < areas.Count; i++)
        {
            for (int j = i + 1; j < areas.Count; j++)
            {
                if (TryGetConnection(areas[i], areas[j], out DoorConnection connection))
                {
                    connections.Add(connection);
                }
            }
        }

        return connections;
    }

    private static bool TryGetConnection(WhiteboxArea a, WhiteboxArea b, out DoorConnection connection)
    {
        connection = null;

        bool hasHorizontalTouch = TryGetHorizontalTouch(a.Bounds, b.Bounds, out Vector2 horizontalCenter, out Vector2 horizontalNormal, out float horizontalSpan);
        bool hasVerticalTouch = TryGetVerticalTouch(a.Bounds, b.Bounds, out Vector2 verticalCenter, out Vector2 verticalNormal, out float verticalSpan);

        if (!hasHorizontalTouch && !hasVerticalTouch)
        {
            return false;
        }

        bool useHorizontal = hasHorizontalTouch && (!hasVerticalTouch || horizontalSpan >= verticalSpan);
        connection = new DoorConnection
        {
            A = a,
            B = b,
            Center = useHorizontal ? horizontalCenter : verticalCenter,
            NormalFromAToB = useHorizontal ? horizontalNormal : verticalNormal,
            VerticalDoor = useHorizontal,
            Span = useHorizontal ? horizontalSpan : verticalSpan
        };

        return connection.Span >= MinimumDoorSpan;
    }

    private static bool TryGetHorizontalTouch(Bounds a, Bounds b, out Vector2 center, out Vector2 normalFromAToB, out float span)
    {
        center = Vector2.zero;
        normalFromAToB = Vector2.zero;
        span = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
        if (span < MinimumDoorSpan)
        {
            return false;
        }

        float aRightToBLeft = Mathf.Abs(a.max.x - b.min.x);
        float bRightToALeft = Mathf.Abs(b.max.x - a.min.x);
        if (aRightToBLeft > EdgeTolerance && bRightToALeft > EdgeTolerance)
        {
            return false;
        }

        if (aRightToBLeft <= bRightToALeft)
        {
            center = new Vector2((a.max.x + b.min.x) * 0.5f, (Mathf.Max(a.min.y, b.min.y) + Mathf.Min(a.max.y, b.max.y)) * 0.5f);
            normalFromAToB = Vector2.right;
        }
        else
        {
            center = new Vector2((b.max.x + a.min.x) * 0.5f, (Mathf.Max(a.min.y, b.min.y) + Mathf.Min(a.max.y, b.max.y)) * 0.5f);
            normalFromAToB = Vector2.left;
        }

        return true;
    }

    private static bool TryGetVerticalTouch(Bounds a, Bounds b, out Vector2 center, out Vector2 normalFromAToB, out float span)
    {
        center = Vector2.zero;
        normalFromAToB = Vector2.zero;
        span = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        if (span < MinimumDoorSpan)
        {
            return false;
        }

        float aTopToBBottom = Mathf.Abs(a.max.y - b.min.y);
        float bTopToABottom = Mathf.Abs(b.max.y - a.min.y);
        if (aTopToBBottom > EdgeTolerance && bTopToABottom > EdgeTolerance)
        {
            return false;
        }

        if (aTopToBBottom <= bTopToABottom)
        {
            center = new Vector2((Mathf.Max(a.min.x, b.min.x) + Mathf.Min(a.max.x, b.max.x)) * 0.5f, (a.max.y + b.min.y) * 0.5f);
            normalFromAToB = Vector2.up;
        }
        else
        {
            center = new Vector2((Mathf.Max(a.min.x, b.min.x) + Mathf.Min(a.max.x, b.max.x)) * 0.5f, (b.max.y + a.min.y) * 0.5f);
            normalFromAToB = Vector2.down;
        }

        return true;
    }

    private static RoomDoor CreateDoor(Transform root, WhiteboxArea owner, WhiteboxArea target, Vector2 contactCenter, Vector2 ownerOffsetDirection, bool verticalDoor, Sprite sprite)
    {
        GameObject doorObject = WhiteboxPrefabUtility.CreateDoor(
            root,
            $"Door_{owner.Transform.name}_To_{target.Transform.name}",
            sprite,
            new Color(0.1f, 0.75f, 1f, 0.55f),
            10).gameObject;
        Undo.RegisterCreatedObjectUndo(doorObject, "Create whitebox door");
        doorObject.transform.SetParent(root, false);
        doorObject.transform.position = contactCenter + ownerOffsetDirection * DoorInset;
        doorObject.transform.rotation = Quaternion.identity;
        doorObject.transform.localScale = verticalDoor
            ? new Vector3(DoorThickness, DoorLength, 1f)
            : new Vector3(DoorLength, DoorThickness, 1f);

        BoxCollider2D collider = WhiteboxPrefabUtility.EnsureComponent<BoxCollider2D>(doorObject);
        collider.isTrigger = true;
        collider.size = Vector2.one;

        SpriteRenderer renderer = WhiteboxPrefabUtility.EnsureComponent<SpriteRenderer>(doorObject);
        renderer.sprite = sprite;
        renderer.color = new Color(0.1f, 0.75f, 1f, 0.55f);
        renderer.sortingOrder = 10;

        return WhiteboxPrefabUtility.EnsureComponent<RoomDoor>(doorObject);
    }

    private static void LinkDoors(RoomDoor sourceDoor, RoomDoor targetDoor)
    {
        SerializedObject serializedDoor = new SerializedObject(sourceDoor);
        serializedDoor.FindProperty("isOpen").boolValue = false;
        serializedDoor.FindProperty("targetSpawn").objectReferenceValue = targetDoor.transform;
        serializedDoor.FindProperty("autoLinkWhenTargetMissing").boolValue = false;
        serializedDoor.FindProperty("exitOffset").floatValue = ExitOffset;
        serializedDoor.ApplyModifiedPropertiesWithoutUndo();
    }
}
