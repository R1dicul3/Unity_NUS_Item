using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Map1TilemapBuilder
{
    private const string ScenePath = "Assets/Scenes/Prototypes/SampleScene.unity";
    private const string AutoRunFlagPath = "Assets/Editor/RunMap1TilemapConversion.flag";
    private const string LegacyWhiteboxRootName = "White_Box";
    private const string TilemapRootName = "Tilemap_Map";
    private const string TileFolder = "Assets/Art/Tiles";
    private const string PixelSpritePath = "Assets/Art/whitebox_pixel.png";

    private const float ImageCenterX = 738f;
    private const float ImageCenterY = 542f;
    private const float PixelsPerCell = 12.5f;
    private const float DoorInset = 0.6f;
    private const float DoorLength = 3f;
    private const float DoorThickness = 1f;
    private const float ExitOffset = 1.25f;

    private static readonly Color FloorColor = new Color(0.20f, 0.24f, 0.30f, 1f);
    private static readonly Color RoomFloorColor = new Color(0.27f, 0.31f, 0.39f, 1f);
    private static readonly Color WallColor = new Color(0.06f, 0.07f, 0.09f, 1f);
    private static readonly Color HiddenPathColor = new Color(0.78f, 0.42f, 0.14f, 1f);
    private static readonly Color DoorColor = new Color(0.1f, 0.75f, 1f, 0.55f);
    private static readonly Color LabelColor = new Color(0.92f, 0.94f, 0.98f, 1f);

    private sealed class AreaSpec
    {
        public string Name;
        public string Label;
        public Rect PixelRect;
        public bool Hidden;
        public bool Room;
    }

    private sealed class AreaBuild
    {
        public AreaSpec Spec;
        public RectInt Rect;
        public Bounds Bounds;
    }

    [MenuItem("Tools/Tilemap/Convert SampleScene Map To Tilemap")]
    public static void ConvertCurrentScene()
    {
        ConvertSceneToTilemap(saveScene: false);
    }

    [InitializeOnLoadMethod]
    private static void ConvertFromFlagWhenEditorReloads()
    {
        if (!File.Exists(AutoRunFlagPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoRunFlagPath))
            {
                return;
            }

            File.Delete(AutoRunFlagPath);
            File.Delete(AutoRunFlagPath + ".meta");
            AssetDatabase.Refresh();
            ConvertSampleScene();
        };
    }

    public static void ConvertSampleScene()
    {
        EditorSceneManager.OpenScene(ScenePath);
        ConvertSceneToTilemap(saveScene: true);
    }

    private static void ConvertSceneToTilemap(bool saveScene)
    {
        Tile floorTile = EnsureTile("Whitebox_Floor.asset", FloorColor, Tile.ColliderType.None);
        Tile roomTile = EnsureTile("Whitebox_RoomFloor.asset", RoomFloorColor, Tile.ColliderType.None);
        Tile wallTile = EnsureTile("Whitebox_Wall.asset", WallColor, Tile.ColliderType.Grid);
        Tile hiddenTile = EnsureTile("Whitebox_HiddenPath.asset", HiddenPathColor, Tile.ColliderType.None);
        Sprite pixelSprite = EnsurePixelSprite();

        RemoveExistingMapRoots();

        GameObject root = new GameObject(TilemapRootName);
        Grid grid = root.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        Tilemap floorMap = CreateTilemap(root.transform, "Floor", 0, false);
        Tilemap hiddenMap = CreateTilemap(root.transform, "Hidden_Paths", 1, false);
        Tilemap wallMap = CreateTilemap(root.transform, "Walls", 2, true);
        Transform areaMarkers = CreateChild(root.transform, "Area_Markers").transform;
        Transform labels = CreateChild(root.transform, "Labels").transform;
        Transform doors = CreateChild(root.transform, "Doors").transform;

        Dictionary<string, AreaBuild> areas = new Dictionary<string, AreaBuild>();
        foreach (AreaSpec spec in CreateAreaSpecs())
        {
            AreaBuild area = BuildArea(spec);
            areas.Add(spec.Name, area);
            PaintArea(area, floorMap, hiddenMap, wallMap, spec.Room ? roomTile : floorTile, hiddenTile, wallTile);
            CreateAreaMarker(areaMarkers, area);

            if (!string.IsNullOrEmpty(spec.Label))
            {
                CreateLabel(labels, area, spec.Label);
            }
        }

        foreach ((string a, string b) in CreateConnections())
        {
            CreateDoorPair(doors, areas, a, b, pixelSprite);
        }

        RebindRoomPillarPuzzle(areas);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"SampleScene tilemap conversion complete: {areas.Count} tilemap areas, {CreateConnections().Count * 2} doors.");
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool collider)
    {
        GameObject tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(parent, false);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (collider)
        {
            Rigidbody2D body = tilemapObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = tilemapObject.AddComponent<Rigidbody2D>();
            }

            TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.usedByComposite = true;

            CompositeCollider2D composite = tilemapObject.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = tilemapObject.AddComponent<CompositeCollider2D>();
            }

            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            body.bodyType = RigidbodyType2D.Static;
        }

        return tilemap;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void CreateAreaMarker(Transform parent, AreaBuild area)
    {
        GameObject marker = new GameObject(area.Spec.Name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = area.Bounds.center;
        marker.transform.localScale = area.Bounds.size;
        marker.hideFlags = HideFlags.NotEditable;
    }

    private static void PaintArea(AreaBuild area, Tilemap floorMap, Tilemap hiddenMap, Tilemap wallMap, TileBase floorTile, TileBase hiddenTile, TileBase wallTile)
    {
        RectInt rect = area.Rect;
        Tilemap targetFloorMap = area.Spec.Hidden ? hiddenMap : floorMap;
        TileBase targetFloorTile = area.Spec.Hidden ? hiddenTile : floorTile;

        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                bool edge = x == rect.xMin || x == rect.xMax - 1 || y == rect.yMin || y == rect.yMax - 1;
                if (edge)
                {
                    wallMap.SetTile(cell, wallTile);
                }
                else
                {
                    targetFloorMap.SetTile(cell, targetFloorTile);
                }
            }
        }
    }

    private static AreaBuild BuildArea(AreaSpec spec)
    {
        RectInt rect = PixelRectToCells(spec.PixelRect);
        rect.width = Mathf.Max(spec.Room ? 5 : 3, rect.width);
        rect.height = Mathf.Max(spec.Room ? 4 : 3, rect.height);

        return new AreaBuild
        {
            Spec = spec,
            Rect = rect,
            Bounds = new Bounds(
                new Vector3(rect.center.x, rect.center.y, 0f),
                new Vector3(rect.width, rect.height, 1f))
        };
    }

    private static RectInt PixelRectToCells(Rect pixelRect)
    {
        Vector2 min = PixelToCell(new Vector2(pixelRect.xMin, pixelRect.yMax));
        Vector2 max = PixelToCell(new Vector2(pixelRect.xMax, pixelRect.yMin));

        int xMin = Mathf.RoundToInt(Mathf.Min(min.x, max.x));
        int xMax = Mathf.RoundToInt(Mathf.Max(min.x, max.x));
        int yMin = Mathf.RoundToInt(Mathf.Min(min.y, max.y));
        int yMax = Mathf.RoundToInt(Mathf.Max(min.y, max.y));

        return new RectInt(xMin, yMin, Mathf.Max(1, xMax - xMin), Mathf.Max(1, yMax - yMin));
    }

    private static Vector2 PixelToCell(Vector2 pixel)
    {
        return new Vector2((pixel.x - ImageCenterX) / PixelsPerCell, (ImageCenterY - pixel.y) / PixelsPerCell);
    }

    private static void CreateLabel(Transform parent, AreaBuild area, string label)
    {
        GameObject labelObject = new GameObject($"Label_{area.Spec.Name}");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = new Vector3(area.Bounds.center.x, area.Bounds.center.y, -0.2f);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = label;
        text.fontSize = area.Spec.Name == "Room_7" ? 30 : 36;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = LabelColor;
    }

    private static void CreateDoorPair(Transform root, Dictionary<string, AreaBuild> areas, string a, string b, Sprite sprite)
    {
        if (!areas.ContainsKey(a) || !areas.ContainsKey(b))
        {
            Debug.LogWarning($"Skipped tilemap door pair {a} <-> {b}: missing area.");
            return;
        }

        AreaBuild areaA = areas[a];
        AreaBuild areaB = areas[b];
        Vector2 direction = GetDominantAxisDirection(areaB.Bounds.center - areaA.Bounds.center);
        Vector2 contactCenter = GetContactCenter(areaA.Bounds, areaB.Bounds, direction);
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
        SetBoolIfPresent(serializedDoor, "isOpen", false);
        SetObjectIfPresent(serializedDoor, "targetSpawn", targetDoor.transform);
        SetBoolIfPresent(serializedDoor, "autoLinkWhenTargetMissing", false);
        SetFloatIfPresent(serializedDoor, "exitOffset", ExitOffset);
        serializedDoor.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RebindRoomPillarPuzzle(Dictionary<string, AreaBuild> areas)
    {
        RoomPillarPuzzle2D puzzle = Object.FindFirstObjectByType<RoomPillarPuzzle2D>();
        if (puzzle == null || !areas.TryGetValue("Room_1", out AreaBuild roomOne))
        {
            return;
        }

        GameObject roomMarker = GameObject.Find("Room_1");
        puzzle.ConfigureRoom(roomMarker != null ? roomMarker.transform : null);

        SerializedObject serializedPuzzle = new SerializedObject(puzzle);
        SetVector2IfPresent(serializedPuzzle, "fallbackRoomCenter", roomOne.Bounds.center);
        SetVector2IfPresent(serializedPuzzle, "fallbackRoomSize", roomOne.Bounds.size);
        serializedPuzzle.ApplyModifiedPropertiesWithoutUndo();

        puzzle.transform.SetParent(null, true);
        puzzle.transform.position = Vector3.zero;
        puzzle.transform.localScale = Vector3.one;
        puzzle.RebuildPuzzle();
    }

    private static void SetBoolIfPresent(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloatIfPresent(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetObjectIfPresent(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetVector2IfPresent(SerializedObject serializedObject, string propertyName, Vector2 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.vector2Value = value;
        }
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
        return new AreaSpec { Name = name, PixelRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax) };
    }

    private static AreaSpec HiddenPath(string name, float xMin, float yMin, float xMax, float yMax)
    {
        return new AreaSpec { Name = name, PixelRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax), Hidden = true };
    }

    private static void RemoveExistingMapRoots()
    {
        DestroyRoot(LegacyWhiteboxRootName);
        DestroyRoot(TilemapRootName);
    }

    private static void DestroyRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static Tile EnsureTile(string assetName, Color color, Tile.ColliderType colliderType)
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Tiles");

        string path = $"{TileFolder}/{assetName}";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = EnsurePixelSprite();
        tile.color = color;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        AssetDatabase.SaveAssets();
        return tile;
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
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 1f))
            {
                importer.spritePixelsPerUnit = 1f;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
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
