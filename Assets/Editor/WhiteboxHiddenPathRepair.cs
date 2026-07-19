using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class WhiteboxHiddenPathRepair
{
    private const string WhiteboxRootName = "White_Box";
    private const float DoorInset = 0.55f;
    private const float DoorThickness = 0.35f;
    private const float DoorLength = 2.8f;
    private const float WallThickness = 0.32f;
    private const float ExitOffset = 1.25f;

    private static readonly Color HiddenPathColor = new Color(0.95f, 0.62f, 0.28f, 0.55f);
    private static readonly Color DoorColor = new Color(0.1f, 0.75f, 1f, 0.55f);
    private static readonly Color WallColor = new Color(0.1f, 0.12f, 0.14f, 1f);
    private static readonly Color DashColor = new Color(0.04f, 0.05f, 0.07f, 1f);

    static WhiteboxHiddenPathRepair()
    {
        EditorApplication.delayCall += RepairIfNeeded;
    }

    [MenuItem("Tools/Whitebox/Repair Path Hide 3 Connector")]
    public static void RepairPathHide3ConnectorFromMenu()
    {
        RepairPathHide3Connector(force: true);
    }

    private static void RepairIfNeeded()
    {
        RepairPathHide3Connector(force: false);
    }

    private static void RepairPathHide3Connector(bool force)
    {
        GameObject whiteboxRoot = GameObject.Find(WhiteboxRootName);
        if (whiteboxRoot == null)
        {
            return;
        }

        Transform hide3 = whiteboxRoot.transform.Find("Path_hide_3");
        Transform path12 = whiteboxRoot.transform.Find("Path_12");
        if (hide3 == null || path12 == null)
        {
            return;
        }

        Transform hide4 = whiteboxRoot.transform.Find("Path_hide_4");
        bool hasOldDirectDoor = whiteboxRoot.transform.Find("Door_Path_hide_3_To_Path_12") != null
            || whiteboxRoot.transform.Find("Door_Path_12_To_Path_hide_3") != null;

        if (!force && hide4 != null && !hasOldDirectDoor)
        {
            NormalizeHiddenPath(hide3, FindReferenceSprite(hide3, path12));
            NormalizeHiddenPath(hide4, FindReferenceSprite(hide3, path12));
            return;
        }

        Sprite sprite = FindReferenceSprite(hide3, path12);
        RemoveConnectorObjects(whiteboxRoot.transform);

        Bounds hide3Bounds = GetAreaBounds(hide3);
        Bounds path12Bounds = GetAreaBounds(path12);
        Bounds connectorBounds = CreateConnectorBounds(hide3Bounds, path12Bounds);

        NormalizeHiddenPath(hide3, sprite);
        hide4 = CreateHiddenConnectorArea(whiteboxRoot.transform, connectorBounds, sprite);

        CreateDoorPair(whiteboxRoot.transform, hide3, hide4, hide3Bounds, connectorBounds, sprite);
        CreateDoorPair(whiteboxRoot.transform, hide4, path12, connectorBounds, path12Bounds, sprite);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Repaired Path_hide_3 closure and filled Path_hide_4 with hidden path color.");
    }

    private static void NormalizeHiddenPath(Transform area, Sprite sprite)
    {
        SpriteRenderer renderer = area.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = area.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.color = HiddenPathColor;
        renderer.sortingOrder = 1;

        Bounds bounds = GetAreaBounds(area);
        float width = Mathf.Max(WallThickness, bounds.size.x);
        float height = Mathf.Max(WallThickness, bounds.size.y);
        float normalizedWallWidth = Mathf.Clamp01(WallThickness / width);
        float normalizedWallHeight = Mathf.Clamp01(WallThickness / height);

        EnsureBoundaryChild(area, "Floor", sprite, new Vector2(1f, normalizedWallHeight), new Vector2(0f, -0.5f + normalizedWallHeight * 0.5f));
        EnsureBoundaryChild(area, "Ceiling", sprite, new Vector2(1f, normalizedWallHeight), new Vector2(0f, 0.5f - normalizedWallHeight * 0.5f));
        EnsureBoundaryChild(area, "Wall_Left", sprite, new Vector2(normalizedWallWidth, 1f), new Vector2(-0.5f + normalizedWallWidth * 0.5f, 0f));
        EnsureBoundaryChild(area, "Wall_Right", sprite, new Vector2(normalizedWallWidth, 1f), new Vector2(0.5f - normalizedWallWidth * 0.5f, 0f));
    }

    private static void EnsureBoundaryChild(Transform area, string childName, Sprite sprite, Vector2 normalizedSize, Vector2 normalizedPosition)
    {
        Transform child = area.Find(childName);
        if (child == null)
        {
            child = WhiteboxPrefabUtility.CreateBoundary(area, childName, sprite, WallColor, 2).transform;
        }

        child.localPosition = new Vector3(normalizedPosition.x, normalizedPosition.y, -0.04f);
        child.localScale = new Vector3(normalizedSize.x, normalizedSize.y, 1f);

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = sprite;
            renderer.color = WallColor;
            renderer.sortingOrder = 2;
        }

        BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = false;
            collider.size = Vector2.one;
        }
    }

    private static void RemoveConnectorObjects(Transform root)
    {
        string[] names =
        {
            "Path_hide_4",
            "Door_Path_hide_3_To_Path_12",
            "Door_Path_12_To_Path_hide_3",
            "Door_Path_hide_3_To_Path_hide_4",
            "Door_Path_hide_4_To_Path_hide_3",
            "Door_Path_hide_4_To_Path_12",
            "Door_Path_12_To_Path_hide_4"
        };

        foreach (string name in names)
        {
            Transform child = root.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static Transform CreateHiddenConnectorArea(Transform parent, Bounds bounds, Sprite sprite)
    {
        GameObject area = new GameObject("Path_hide_4");
        area.transform.SetParent(parent, false);
        area.transform.position = bounds.center;
        area.transform.localScale = bounds.size;

        SpriteRenderer renderer = area.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = HiddenPathColor;
        renderer.sortingOrder = 1;

        CreateBoundaryColliders(area.transform, sprite, bounds);
        CreateHiddenPathDashes(area.transform, sprite, bounds);
        return area.transform;
    }

    private static void CreateBoundaryColliders(Transform parent, Sprite sprite, Bounds bounds)
    {
        float width = Mathf.Max(WallThickness, bounds.size.x);
        float height = Mathf.Max(WallThickness, bounds.size.y);
        float normalizedWallWidth = Mathf.Clamp01(WallThickness / width);
        float normalizedWallHeight = Mathf.Clamp01(WallThickness / height);

        CreateBoundaryChild(parent, "Floor", sprite, new Vector2(1f, normalizedWallHeight), new Vector2(0f, -0.5f + normalizedWallHeight * 0.5f));
        CreateBoundaryChild(parent, "Ceiling", sprite, new Vector2(1f, normalizedWallHeight), new Vector2(0f, 0.5f - normalizedWallHeight * 0.5f));
        CreateBoundaryChild(parent, "Wall_Left", sprite, new Vector2(normalizedWallWidth, 1f), new Vector2(-0.5f + normalizedWallWidth * 0.5f, 0f));
        CreateBoundaryChild(parent, "Wall_Right", sprite, new Vector2(normalizedWallWidth, 1f), new Vector2(0.5f - normalizedWallWidth * 0.5f, 0f));
    }

    private static void CreateBoundaryChild(Transform parent, string name, Sprite sprite, Vector2 normalizedSize, Vector2 normalizedPosition)
    {
        GameObject child = WhiteboxPrefabUtility.CreateBoundary(parent, name, sprite, WallColor, 2);
        child.transform.localPosition = new Vector3(normalizedPosition.x, normalizedPosition.y, -0.04f);
        child.transform.localScale = new Vector3(normalizedSize.x, normalizedSize.y, 1f);
    }

    private static void CreateHiddenPathDashes(Transform parent, Sprite sprite, Bounds pathBounds)
    {
        bool vertical = pathBounds.size.y >= pathBounds.size.x;
        int dashCount = Mathf.Max(3, Mathf.RoundToInt((vertical ? pathBounds.size.y : pathBounds.size.x) / 1.4f));

        for (int i = 0; i < dashCount; i++)
        {
            float t = dashCount == 1 ? 0.5f : i / (float)(dashCount - 1);
            GameObject dash = new GameObject($"Dash_{i + 1:00}");
            dash.transform.SetParent(parent, false);
            dash.transform.localPosition = vertical
                ? new Vector3(0f, Mathf.Lerp(-0.42f, 0.42f, t), -0.08f)
                : new Vector3(Mathf.Lerp(-0.42f, 0.42f, t), 0f, -0.08f);
            dash.transform.localScale = vertical ? new Vector3(0.8f, 0.08f, 1f) : new Vector3(0.08f, 0.8f, 1f);

            SpriteRenderer renderer = dash.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = DashColor;
            renderer.sortingOrder = 5;
        }
    }

    private static void CreateDoorPair(Transform root, Transform a, Transform b, Bounds aBounds, Bounds bBounds, Sprite sprite)
    {
        Vector2 direction = GetDominantAxisDirection(bBounds.center - aBounds.center);
        Vector2 contactCenter = GetContactCenter(aBounds, bBounds, direction);
        bool verticalDoor = Mathf.Abs(direction.x) > 0.01f;

        RoomDoor doorA = CreateDoor(root, a.name, b.name, contactCenter - direction * DoorInset, verticalDoor, sprite);
        RoomDoor doorB = CreateDoor(root, b.name, a.name, contactCenter + direction * DoorInset, verticalDoor, sprite);
        LinkDoors(doorA, doorB);
        LinkDoors(doorB, doorA);
    }

    private static RoomDoor CreateDoor(Transform root, string source, string target, Vector2 position, bool verticalDoor, Sprite sprite)
    {
        RoomDoor door = WhiteboxPrefabUtility.CreateDoor(root, $"Door_{source}_To_{target}", sprite, DoorColor, 10);
        door.transform.position = position;
        door.transform.localScale = verticalDoor
            ? new Vector3(DoorThickness, DoorLength, 1f)
            : new Vector3(DoorLength, DoorThickness, 1f);
        return door;
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

    private static Bounds CreateConnectorBounds(Bounds hide3Bounds, Bounds path12Bounds)
    {
        float xMin = hide3Bounds.max.x;
        float xMax = path12Bounds.min.x;
        float yMin = Mathf.Max(hide3Bounds.min.y, path12Bounds.min.y);
        float yMax = Mathf.Min(hide3Bounds.max.y, path12Bounds.max.y);
        Vector3 center = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
        Vector3 size = new Vector3(Mathf.Max(0.1f, xMax - xMin), Mathf.Max(0.1f, yMax - yMin), 1f);
        return new Bounds(center, size);
    }

    private static Bounds GetAreaBounds(Transform area)
    {
        return new Bounds(area.position, new Vector3(Mathf.Abs(area.lossyScale.x), Mathf.Abs(area.lossyScale.y), 1f));
    }

    private static Sprite FindReferenceSprite(params Transform[] references)
    {
        foreach (Transform reference in references)
        {
            SpriteRenderer renderer = reference.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return null;
    }

    private static Vector2 GetContactCenter(Bounds a, Bounds b, Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            float x = direction.x > 0f ? (a.max.x + b.min.x) * 0.5f : (a.min.x + b.max.x) * 0.5f;
            float yMin = Mathf.Max(a.min.y, b.min.y);
            float yMax = Mathf.Min(a.max.y, b.max.y);
            float y = yMin <= yMax ? (yMin + yMax) * 0.5f : (a.center.y + b.center.y) * 0.5f;
            return new Vector2(x, y);
        }

        float yContact = direction.y > 0f ? (a.max.y + b.min.y) * 0.5f : (a.min.y + b.max.y) * 0.5f;
        float xMin = Mathf.Max(a.min.x, b.min.x);
        float xMax = Mathf.Min(a.max.x, b.max.x);
        float xContact = xMin <= xMax ? (xMin + xMax) * 0.5f : (a.center.x + b.center.x) * 0.5f;
        return new Vector2(xContact, yContact);
    }

    private static Vector2 GetDominantAxisDirection(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }
}
