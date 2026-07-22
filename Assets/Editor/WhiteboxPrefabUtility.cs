using UnityEditor;
using UnityEngine;

internal static class WhiteboxPrefabUtility
{
    public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    public const string RoomDoorPrefabPath = "Assets/Prefabs/RoomDoor.prefab";
    public const string BoundaryPrefabPath = "Assets/Prefabs/WhiteboxBoundary.prefab";

    public static GameObject InstantiatePrefabOrNew(string prefabPath, string fallbackName, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = prefab != null
            ? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject
            : new GameObject(fallbackName);

        if (instance == null)
        {
            instance = new GameObject(fallbackName);
        }

        instance.name = fallbackName;
        if (instance.transform.parent != parent)
        {
            instance.transform.SetParent(parent, false);
        }

        return instance;
    }

    public static GameObject CreateBoundary(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject boundary = InstantiatePrefabOrNew(BoundaryPrefabPath, name, parent);

        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(boundary);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(boundary);
        collider.isTrigger = false;
        collider.size = Vector2.one;

        return boundary;
    }

    public static RoomDoor CreateDoor(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject door = InstantiatePrefabOrNew(RoomDoorPrefabPath, name, parent);

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(door);
        collider.isTrigger = true;
        collider.size = Vector2.one;

        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(door);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        return EnsureComponent<RoomDoor>(door);
    }

    public static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
