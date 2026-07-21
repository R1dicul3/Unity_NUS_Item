using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Scene2TilemapBuilder
{
    private const string ScenePath = "Assets/Scenes/Gameplay/Scene_2.unity";
    private const string AutoRunFlagPath = "Assets/Editor/RunScene2TilemapConversion.flag";
    private const string TilemapRootName = "Tilemap_Scene_2";
    private const string LegacyRootName = "White_Box";
    private const string TileFolder = "Assets/Art/Tiles";
    private const string PixelSpritePath = "Assets/Art/whitebox_pixel.png";
    private const float CellSize = 1f;

    private static readonly Color DefaultStaticColor = new Color(0.1f, 0.12f, 0.14f, 1f);
    private static readonly Color RoomFillColor = new Color(0.16f, 0.19f, 0.24f, 0.35f);
    private static readonly HashSet<string> ExcludedNames = new HashSet<string>
    {
        "Player",
        "Visual",
        "Main Camera",
        "Global Light 2D",
        "DialogueCanvas",
        "Dialogue Module",
        "CoffeeManager",
        "EmotionManager",
        "Background_Color_Lines",
        "LevelVariantSwitcher",
        "Character Ability Switcher"
    };

    [MenuItem("Tools/Tilemap/Convert Scene_2 Rooms And Mechanics To Tilemap")]
    public static void ConvertCurrentScene()
    {
        ConvertSceneToTilemap(saveScene: false);
    }

    [MenuItem("Tools/Tilemap/Convert Scene_2 Rooms And Mechanics To Tilemap And Save")]
    public static void ConvertScene2AndSave()
    {
        EditorSceneManager.OpenScene(ScenePath);
        ConvertSceneToTilemap(saveScene: true);
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
            ConvertScene2AndSave();
        };
    }

    private static void ConvertSceneToTilemap(bool saveScene)
    {
        EnsurePixelSprite();
        RemoveExistingTilemapRoot();

        GameObject root = new GameObject(TilemapRootName);
        Grid grid = root.AddComponent<Grid>();
        grid.cellSize = Vector3.one * CellSize;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        Tilemap staticMap = CreateTilemap(root.transform, "Static_Rooms_And_Platforms", 0, true);
        Tilemap roomMap = CreateTilemap(root.transform, "Room_Fills", -2, false);
        Transform dynamicRoot = CreateChild(root.transform, "Dynamic_Mechanism_Tilemaps").transform;

        Dictionary<Color32, Tile> tileCache = new Dictionary<Color32, Tile>();
        int staticCount = 0;
        int dynamicCount = 0;
        int roomFillCount = 0;

        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ShouldSkip(renderer))
            {
                continue;
            }

            if (IsRoomMarker(renderer))
            {
                PaintRendererBounds(roomMap, renderer, GetTile(tileCache, "Scene2_RoomFill", RoomFillColor, Tile.ColliderType.None));
                renderer.enabled = false;
                roomFillCount++;
                continue;
            }

            BoxCollider2D box = renderer.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                continue;
            }

            bool dynamic = HasMechanicBehaviour(renderer.gameObject);
            Color color = renderer.color;
            Tile tile = GetTile(tileCache, $"Scene2_{GetColorName(color)}", color, dynamic ? Tile.ColliderType.None : Tile.ColliderType.Grid);

            if (dynamic)
            {
                CreateDynamicTilemap(dynamicRoot, renderer, box, tile);
                renderer.enabled = false;
                dynamicCount++;
            }
            else
            {
                PaintRendererBounds(staticMap, renderer, tile);
                renderer.enabled = false;
                box.enabled = false;
                staticCount++;
            }
        }

        RebindCameraToTilemap();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"Scene_2 tilemap conversion complete: {staticCount} static blocks, {roomFillCount} room fills, {dynamicCount} mechanism visuals.");
    }

    private static bool ShouldSkip(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.gameObject == null)
        {
            return true;
        }

        if (ExcludedNames.Contains(renderer.gameObject.name))
        {
            return true;
        }

        if (renderer.GetComponentInParent<Canvas>() != null ||
            renderer.GetComponentInParent<RoomDoor>() != null ||
            renderer.GetComponentInParent<PlatformerPlayerController>() != null ||
            renderer.GetComponentInParent<Coffee>() != null)
        {
            return true;
        }

        return renderer.transform.IsChildOfName(TilemapRootName);
    }

    private static bool IsRoomMarker(SpriteRenderer renderer)
    {
        string name = renderer.gameObject.name;
        return name.StartsWith("Room_");
    }

    private static bool HasMechanicBehaviour(GameObject gameObject)
    {
        return gameObject.GetComponent<MovingPlatform>() != null ||
               gameObject.GetComponent<SinkingPillarSegment2D>() != null ||
               gameObject.GetComponent<SharedPlatformsReveal>() != null ||
               gameObject.GetComponentInParent<SinkingPillar2D>() != null ||
               gameObject.GetComponentInParent<RoomPillarPuzzle2D>() != null;
    }

    private static void CreateDynamicTilemap(Transform parent, SpriteRenderer source, BoxCollider2D sourceBox, TileBase tile)
    {
        GameObject container = new GameObject(source.gameObject.name + "_Tilemap");
        container.transform.SetParent(parent, true);
        container.transform.position = source.transform.position;
        container.transform.rotation = source.transform.rotation;
        container.transform.localScale = source.transform.lossyScale;

        Grid grid = container.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap tilemap = CreateTilemap(container.transform, "Tiles", source.sortingOrder, false);
        Vector2 size = sourceBox.size;
        Vector2 offset = sourceBox.offset;
        int width = Mathf.Max(1, Mathf.RoundToInt(size.x));
        int height = Mathf.Max(1, Mathf.RoundToInt(size.y));
        int xMin = Mathf.FloorToInt(offset.x - width * 0.5f);
        int yMin = Mathf.FloorToInt(offset.y - height * 0.5f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilemap.SetTile(new Vector3Int(xMin + x, yMin + y, 0), tile);
            }
        }
    }

    private static void PaintRendererBounds(Tilemap tilemap, SpriteRenderer renderer, TileBase tile)
    {
        Bounds bounds = renderer.bounds;
        int xMin = Mathf.FloorToInt(bounds.min.x / CellSize);
        int xMax = Mathf.CeilToInt(bounds.max.x / CellSize);
        int yMin = Mathf.FloorToInt(bounds.min.y / CellSize);
        int yMax = Mathf.CeilToInt(bounds.max.y / CellSize);

        for (int x = xMin; x < xMax; x++)
        {
            for (int y = yMin; y < yMax; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
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
            Rigidbody2D body = tilemapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.usedByComposite = true;

            CompositeCollider2D composite = tilemapObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        return tilemap;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Tile GetTile(Dictionary<Color32, Tile> cache, string name, Color color, Tile.ColliderType colliderType)
    {
        Color32 key = color;
        if (cache.TryGetValue(key, out Tile cached))
        {
            return cached;
        }

        Tile tile = EnsureTile(name + ".asset", color, colliderType);
        cache.Add(key, tile);
        return tile;
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

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 1f))
            {
                importer.spritePixelsPerUnit = 1f;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PixelSpritePath);
    }

    private static string GetColorName(Color color)
    {
        Color32 c = color;
        return $"{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
    }

    private static void RebindCameraToTilemap()
    {
        CameraFollow2D cameraFollow = Object.FindFirstObjectByType<CameraFollow2D>(FindObjectsInactive.Include);
        if (cameraFollow == null)
        {
            return;
        }

        SerializedObject serializedCamera = new SerializedObject(cameraFollow);
        SerializedProperty whiteboxName = serializedCamera.FindProperty("whiteboxRootName");
        if (whiteboxName != null)
        {
            whiteboxName.stringValue = TilemapRootName;
        }

        serializedCamera.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cameraFollow);
    }

    private static void RemoveExistingTilemapRoot()
    {
        GameObject root = GameObject.Find(TilemapRootName);
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static bool IsChildOfName(this Transform transform, string parentName)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == parentName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
