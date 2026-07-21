using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Map1WhiteboxBuilder
{
    private const string ScenePath = "Assets/Scenes/Prototypes/SampleScene.unity";
    private const string WhiteboxRootName = "White_Box";
    private const string PixelSpritePath = "Assets/Art/whitebox_pixel.png";

    private const float ImageCenterX = 738f;
    private const float ImageCenterY = 542f;
    private const float PixelsToWorld = 0.08f;
    private const float DoorInset = 0.55f;
    private const float DoorThickness = 0.8f;
    private const float DoorLength = 2.8f;
    private const float LabelZ = -0.2f;
    private const float WallThickness = 0.32f;

    private static readonly Color RoomColor = new Color(0.92f, 0.95f, 1f, 0.88f);
    private static readonly Color PathColor = new Color(0.72f, 0.82f, 0.92f, 0.72f);
    private static readonly Color HiddenPathColor = new Color(0.95f, 0.62f, 0.28f, 0.55f);
    private static readonly Color DoorColor = new Color(0.1f, 0.75f, 1f, 0.55f);
    private static readonly Color WallColor = new Color(0.1f, 0.12f, 0.14f, 1f);
    private static readonly Color LabelColor = new Color(0.04f, 0.05f, 0.07f, 1f);

    private sealed class AreaSpec
    {
        public string Name;
        public string Label;
        public Rect PixelRect;
        public bool Hidden;
        public bool Room;
    }

    [MenuItem("Tools/Whitebox/Rebuild From Documents Map 1")]
    public static void RebuildCurrentSceneFromMap1()
    {
        RebuildMap1InScene(saveScene: false);
    }

    public static void RebuildSampleSceneFromMap1()
    {
        EditorSceneManager.OpenScene(ScenePath);
        RebuildMap1InScene(saveScene: true);
    }

    private static void RebuildMap1InScene(bool saveScene)
    {
        Sprite sprite = EnsurePixelSprite();
        GameObject whiteboxRoot = GameObject.Find(WhiteboxRootName);
        if (whiteboxRoot == null)
        {
            whiteboxRoot = new GameObject(WhiteboxRootName);
        }

        ClearWhiteboxRoot(whiteboxRoot.transform);
        Dictionary<string, Transform> areas = new Dictionary<string, Transform>();
        Dictionary<string, Bounds> bounds = new Dictionary<string, Bounds>();

        foreach (AreaSpec spec in CreateAreaSpecs())
        {
            Transform area = CreateArea(whiteboxRoot.transform, spec, sprite);
            areas.Add(spec.Name, area);
            bounds.Add(spec.Name, CalculateAreaBounds(spec.PixelRect));
        }

        foreach ((string a, string b) in CreateConnections())
        {
            CreateDoorPair(whiteboxRoot.transform, areas, bounds, a, b, sprite);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"Map 1 whitebox rebuild complete: {areas.Count} areas, {CreateConnections().Count * 2} doors.");
    }

    private static List<AreaSpec> CreateAreaSpecs()
    {
        return new List<AreaSpec>
        {
            Room("Room_Start", "Tutorial", 379f, 399f, 627f, 557f),
            Room("Room_1", "P1", 200f, 661f, 600f, 896f),
            Room("Room_2", "P2", 662f, 399f, 938f, 557f),
            Room("Room_3", "P3", 200f, 225f, 334f, 669f),
            Room("Room_4", "?", 984f, 328f, 1355f, 557f),
            Room("Room_5", "P4", 662f, 661f, 930f, 887f),
            Room("Room_6", "P6", 241f, 66f, 446f, 188f),
            Room("Room_7", "Hidden Room", 662f, 225f, 938f, 361f),

            Path("Path_1", 121f, 66f, 241f, 112f),
            Path("Path_2", 446f, 73f, 611f, 112f),
            Path("Path_3", 423f, 184f, 434f, 253f),
            Path("Path_4", 331f, 241f, 434f, 253f),
            Path("Path_5", 491f, 112f, 506f, 405f),
            Path("Path_6", 627f, 512f, 662f, 532f),
            Path("Path_7", 938f, 512f, 984f, 532f),
            Path("Path_8", 596f, 740f, 662f, 758f),
            Path("Path_9", 930f, 815f, 1072f, 840f),
            Path("Path_10", 1058f, 557f, 1072f, 840f),
            Path("Path_11", 1245f, 557f, 1285f, 979f),
            Path("Path_12", 970f, 975f, 1285f, 1018f),

            HiddenPath("Path_hide_1", 379f, 550f, 396f, 669f),
            HiddenPath("Path_hide_2", 747f, 355f, 766f, 399f),
            HiddenPath("Path_hide_3", 777f, 815f, 800f, 1018f),
            HiddenPath("Path_hide_4", 800f, 975f, 970f, 1018f),
        };
    }

    private static List<(string A, string B)> CreateConnections()
    {
        return new List<(string A, string B)>
        {
            ("Path_1", "Room_6"),
            ("Room_6", "Path_2"),
            ("Room_6", "Path_3"),
            ("Path_3", "Path_4"),
            ("Path_4", "Room_3"),
            ("Path_2", "Path_5"),
            ("Path_5", "Room_Start"),
            ("Room_3", "Room_1"),
            ("Room_Start", "Path_hide_1"),
            ("Path_hide_1", "Room_1"),
            ("Room_Start", "Path_6"),
            ("Path_6", "Room_2"),
            ("Room_2", "Path_hide_2"),
            ("Path_hide_2", "Room_7"),
            ("Room_2", "Path_7"),
            ("Path_7", "Room_4"),
            ("Room_1", "Path_8"),
            ("Path_8", "Room_5"),
            ("Room_5", "Path_9"),
            ("Path_9", "Path_10"),
            ("Path_10", "Room_4"),
            ("Room_4", "Path_11"),
            ("Path_11", "Path_12"),
            ("Room_5", "Path_hide_3"),
            ("Path_hide_3", "Path_hide_4"),
            ("Path_hide_4", "Path_12"),
        };
    }

    private static AreaSpec Room(string name, string label, float xMin, float yMin, float xMax, float yMax)
    {
        return new AreaSpec { Name = name, Label = label, PixelRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax), Room = true };
    }

    private static AreaSpec Path(string name, float xMin, float yMin, float xMax, float yMax)
    {
        return new AreaSpec { Name = name, PixelRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax), Room = false };
    }

    private static AreaSpec HiddenPath(string name, float xMin, float yMin, float xMax, float yMax)
    {
        return new AreaSpec { Name = name, PixelRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax), Hidden = true };
    }

    private static Transform CreateArea(Transform parent, AreaSpec spec, Sprite sprite)
    {
        Bounds bounds = CalculateAreaBounds(spec.PixelRect);
        GameObject areaObject = new GameObject(spec.Name);
        areaObject.transform.SetParent(parent, false);
        areaObject.transform.position = bounds.center;
        areaObject.transform.localScale = bounds.size;

        SpriteRenderer renderer = areaObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = spec.Hidden ? HiddenPathColor : spec.Room ? RoomColor : PathColor;
        renderer.sortingOrder = spec.Room ? 0 : 1;

        CreateBoundaryColliders(areaObject.transform, sprite, bounds);

        if (!string.IsNullOrEmpty(spec.Label))
        {
            CreateLabel(areaObject.transform, spec.Label, spec.Room && spec.Name == "Room_7" ? 34 : 42);
        }

        if (spec.Hidden)
        {
            CreateHiddenPathDashes(areaObject.transform, sprite, bounds);
        }

        if (spec.Name == "Room_Start")
        {
            CreateTutorialMarkers(areaObject.transform, sprite, bounds);
        }
        else if (spec.Name == "Room_5")
        {
            CreatePuzzleRoomSteps(areaObject.transform, sprite, bounds);
        }

        return areaObject.transform;
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
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(normalizedPosition.x, normalizedPosition.y, -0.04f);
        child.transform.localScale = new Vector3(normalizedSize.x, normalizedSize.y, 1f);
    }

    private static void CreateLabel(Transform parent, string label, int fontSize)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, LabelZ);
        labelObject.transform.localScale = Vector3.one;

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = label;
        text.fontSize = fontSize;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = LabelColor;
    }

    private static void CreateTutorialMarkers(Transform parent, Sprite sprite, Bounds roomBounds)
    {
        CreateVisualChild(parent, "EndMarker", sprite, new Vector2(0.38f, 0.24f), new Vector2(0.38f, 0.2f), Color.black, 4);
        CreateVisualChild(parent, "FloorMarker", sprite, new Vector2(0.12f, 0.03f), new Vector2(0.12f, -0.43f), Color.black, 4);
    }

    private static void CreatePuzzleRoomSteps(Transform parent, Sprite sprite, Bounds roomBounds)
    {
        CreateVisualChild(parent, "Step_UpperLeft", sprite, new Vector2(0.34f, 0.16f), new Vector2(-0.23f, 0.3f), LabelColor, 3);
        CreateVisualChild(parent, "Step_Middle", sprite, new Vector2(0.34f, 0.16f), new Vector2(0.1f, 0.02f), LabelColor, 3);
        CreateVisualChild(parent, "Step_LowerRight", sprite, new Vector2(0.34f, 0.16f), new Vector2(0.28f, -0.32f), LabelColor, 3);
    }

    private static void CreateVisualChild(Transform parent, string name, Sprite sprite, Vector2 normalizedSize, Vector2 normalizedPosition, Color color, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(normalizedPosition.x, normalizedPosition.y, -0.05f);
        child.transform.localScale = new Vector3(normalizedSize.x, normalizedSize.y, 1f);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
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
            renderer.color = LabelColor;
            renderer.sortingOrder = 5;
        }
    }

    private static void CreateDoorPair(Transform root, Dictionary<string, Transform> areas, Dictionary<string, Bounds> bounds, string a, string b, Sprite sprite)
    {
        if (!areas.ContainsKey(a) || !areas.ContainsKey(b))
        {
            Debug.LogWarning($"Skipped door pair {a} <-> {b}: missing area.");
            return;
        }

        Bounds aBounds = bounds[a];
        Bounds bBounds = bounds[b];
        Vector2 direction = GetDominantAxisDirection(bBounds.center - aBounds.center);
        Vector2 contactCenter = GetContactCenter(aBounds, bBounds, direction);
        bool verticalDoor = Mathf.Abs(direction.x) > 0.01f;

        RoomDoor doorA = CreateDoor(root, a, b, contactCenter - direction * DoorInset, verticalDoor, sprite);
        RoomDoor doorB = CreateDoor(root, b, a, contactCenter + direction * DoorInset, verticalDoor, sprite);
        LinkDoors(doorA, doorB);
        LinkDoors(doorB, doorA);
    }

    private static RoomDoor CreateDoor(Transform root, string source, string target, Vector2 position, bool verticalDoor, Sprite sprite)
    {
        GameObject doorObject = WhiteboxPrefabUtility.CreateDoor(root, $"Door_{source}_To_{target}", sprite, DoorColor, 10).gameObject;
        doorObject.transform.SetParent(root, false);
        doorObject.transform.position = position;
        doorObject.transform.localScale = verticalDoor
            ? new Vector3(DoorThickness, DoorLength, 1f)
            : new Vector3(DoorLength, DoorThickness, 1f);

        BoxCollider2D collider = WhiteboxPrefabUtility.EnsureComponent<BoxCollider2D>(doorObject);
        collider.isTrigger = true;
        collider.size = Vector2.one;

        SpriteRenderer renderer = WhiteboxPrefabUtility.EnsureComponent<SpriteRenderer>(doorObject);
        renderer.sprite = sprite;
        renderer.color = DoorColor;
        renderer.sortingOrder = 10;

        return WhiteboxPrefabUtility.EnsureComponent<RoomDoor>(doorObject);
    }

    private static void LinkDoors(RoomDoor sourceDoor, RoomDoor targetDoor)
    {
        SerializedObject serializedDoor = new SerializedObject(sourceDoor);
        serializedDoor.FindProperty("isOpen").boolValue = false;
        serializedDoor.FindProperty("targetSpawn").objectReferenceValue = targetDoor.transform;
        serializedDoor.FindProperty("autoLinkWhenTargetMissing").boolValue = false;
        serializedDoor.FindProperty("exitOffset").floatValue = 1.25f;
        serializedDoor.ApplyModifiedPropertiesWithoutUndo();
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

    private static Bounds CalculateAreaBounds(Rect pixelRect)
    {
        Vector2 centerPixel = pixelRect.center;
        Vector2 center = PixelToWorld(centerPixel);
        Vector3 size = new Vector3(pixelRect.width * PixelsToWorld, pixelRect.height * PixelsToWorld, 1f);
        return new Bounds(center, size);
    }

    private static Vector2 PixelToWorld(Vector2 pixel)
    {
        return new Vector2((pixel.x - ImageCenterX) * PixelsToWorld, (ImageCenterY - pixel.y) * PixelsToWorld);
    }

    private static void ClearWhiteboxRoot(Transform root)
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in root)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children)
        {
            Object.DestroyImmediate(child);
        }
    }

    private static Sprite EnsurePixelSprite()
    {
        EnsureFolder("Assets", "Art");
        if (!File.Exists(PixelSpritePath))
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(PixelSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PixelSpritePath);
        }

        TextureImporter importer = AssetImporter.GetAtPath(PixelSpritePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1f;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PixelSpritePath);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
