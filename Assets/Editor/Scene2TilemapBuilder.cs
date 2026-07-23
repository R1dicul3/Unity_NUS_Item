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
    private const string WhiteboxRootName = "White_Box";
    private const string TilemapRootName = "Tilemap_Scene_2";
    private const string SolidTilemapName = "Solid_Walls_Floors_Ceilings";
    private const string TileFolder = "Assets/Art/Tiles";
    private const string PixelSpritePath = "Assets/Art/whitebox_pixel.png";
    private const string SolidTilePath = TileFolder + "/Scene2_SolidWhitebox.asset";
    private const float CellSize = 0.25f;

    [MenuItem("Tools/Tilemap/Convert Scene_2 Walls Floors Ceilings To Tilemap")]
    public static void ConvertCurrentScene()
    {
        ConvertSceneToTilemap(saveScene: false);
    }

    [MenuItem("Tools/Tilemap/Convert Scene_2 Walls Floors Ceilings To Tilemap And Save")]
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
        Tile solidTile = EnsureSolidTile();
        RemoveExistingTilemapRoot();

        GameObject root = new GameObject(TilemapRootName);
        Grid grid = root.AddComponent<Grid>();
        grid.cellSize = Vector3.one * CellSize;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        Tilemap solidMap = CreateSolidTilemap(root.transform);

        int convertedCount = 0;
        foreach (SpriteRenderer renderer in FindStructuralRenderers())
        {
            PaintRendererBounds(solidMap, renderer, solidTile);
            DisableSourceBlock(renderer);
            convertedCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"Scene_2 structural tilemap conversion complete: {convertedCount} wall/floor/ceiling blocks converted with visible tiles and solid collision.");
    }

    private static IEnumerable<SpriteRenderer> FindStructuralRenderers()
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer == null || renderer.gameObject == null)
            {
                continue;
            }

            Transform transform = renderer.transform;
            if (!transform.IsChildOfName(WhiteboxRootName) || transform.IsChildOfName(TilemapRootName))
            {
                continue;
            }

            if (!IsStructuralName(renderer.gameObject.name))
            {
                continue;
            }

            yield return renderer;
        }
    }

    private static bool IsStructuralName(string objectName)
    {
        return objectName.StartsWith("Floor") ||
               objectName.StartsWith("Ceiling") ||
               objectName.StartsWith("Wall");
    }

    private static Tilemap CreateSolidTilemap(Transform parent)
    {
        GameObject tilemapObject = new GameObject(SolidTilemapName);
        tilemapObject.transform.SetParent(parent, false);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 2;

        Rigidbody2D body = tilemapObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.usedByComposite = true;

        CompositeCollider2D composite = tilemapObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        return tilemap;
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
                Vector3Int cell = new Vector3Int(x, y, 0);
                tilemap.SetTile(cell, tile);
                tilemap.SetTileFlags(cell, TileFlags.None);
                tilemap.SetColor(cell, renderer.color);
            }
        }
    }

    private static void DisableSourceBlock(SpriteRenderer renderer)
    {
        renderer.enabled = false;

        foreach (BoxCollider2D collider in renderer.GetComponents<BoxCollider2D>())
        {
            collider.enabled = false;
        }
    }

    private static Tile EnsureSolidTile()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Tiles");

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(SolidTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, SolidTilePath);
        }

        tile.sprite = EnsurePixelSprite();
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
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
