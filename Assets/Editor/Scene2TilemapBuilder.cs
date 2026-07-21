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
    private const string RoomsRootName = "Rooms";
    private const string TileFolder = "Assets/Art/Tiles";
    private const string PixelSpritePath = "Assets/Art/whitebox_pixel.png";
    private const string RoomTilePath = TileFolder + "/Scene2_Room.asset";

    [MenuItem("Tools/Tilemap/Convert Scene_2 Rooms To Tilemap")]
    public static void ConvertCurrentScene()
    {
        ConvertSceneToRoomTilemap(saveScene: false);
    }

    [MenuItem("Tools/Tilemap/Convert Scene_2 Rooms To Tilemap And Save")]
    public static void ConvertScene2AndSave()
    {
        EditorSceneManager.OpenScene(ScenePath);
        ConvertSceneToRoomTilemap(saveScene: true);
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

    private static void ConvertSceneToRoomTilemap(bool saveScene)
    {
        Tile roomTile = EnsureRoomTile();
        RemoveExistingTilemapRoot();

        GameObject root = new GameObject(TilemapRootName);
        Transform roomsRoot = CreateChild(root.transform, RoomsRootName).transform;

        int roomCount = 0;
        foreach (SpriteRenderer roomRenderer in FindRoomRenderers())
        {
            CreateRoomTilemap(roomsRoot, roomRenderer, roomTile);
            roomRenderer.enabled = false;
            roomCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"Scene_2 room tilemap conversion complete: {roomCount} rooms converted. Platforms and mechanics were not modified.");
    }

    private static IEnumerable<SpriteRenderer> FindRoomRenderers()
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer == null || renderer.gameObject == null)
            {
                continue;
            }

            if (!renderer.gameObject.name.StartsWith("Room_"))
            {
                continue;
            }

            if (renderer.transform.IsChildOfName(TilemapRootName))
            {
                continue;
            }

            yield return renderer;
        }
    }

    private static void CreateRoomTilemap(Transform parent, SpriteRenderer source, TileBase roomTile)
    {
        Bounds bounds = source.bounds;
        int cellsX = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x));
        int cellsY = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y));
        Vector3 tilemapScale = new Vector3(bounds.size.x / cellsX, bounds.size.y / cellsY, 1f);

        GameObject roomObject = new GameObject(source.gameObject.name);
        roomObject.transform.SetParent(parent, false);
        roomObject.transform.position = new Vector3(bounds.min.x, bounds.min.y, source.transform.position.z);
        roomObject.transform.localScale = tilemapScale;

        Grid grid = roomObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        GameObject tilemapObject = new GameObject("Tilemap");
        tilemapObject.transform.SetParent(roomObject.transform, false);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                tilemap.SetTile(cell, roomTile);
                tilemap.SetTileFlags(cell, TileFlags.None);
                tilemap.SetColor(cell, source.color);
            }
        }

        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
    }

    private static Tile EnsureRoomTile()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Tiles");

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(RoomTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, RoomTilePath);
        }

        tile.sprite = EnsurePixelSprite();
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;
        tile.flags = TileFlags.None;
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

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
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
