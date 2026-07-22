using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypePrefabSceneTools
{
    [MenuItem("Tools/Prefabs/Replace Scene Player With Player Prefab")]
    public static void ReplaceScenePlayerWithPrefab()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WhiteboxPrefabUtility.PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogWarning($"Player prefab missing at {WhiteboxPrefabUtility.PlayerPrefabPath}.");
            return;
        }

        PlatformerPlayerController existingPlayer = Object.FindFirstObjectByType<PlatformerPlayerController>();
        Vector3 position = existingPlayer != null ? existingPlayer.transform.position : Vector3.zero;
        Quaternion rotation = existingPlayer != null ? existingPlayer.transform.rotation : Quaternion.identity;
        Transform parent = existingPlayer != null ? existingPlayer.transform.parent : null;

        if (existingPlayer != null)
        {
            Undo.DestroyObjectImmediate(existingPlayer.gameObject);
        }

        GameObject playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab, parent) as GameObject;
        if (playerInstance == null)
        {
            Debug.LogWarning("Failed to instantiate Player prefab.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(playerInstance, "Replace scene player with prefab");
        playerInstance.name = "Player";
        playerInstance.transform.SetPositionAndRotation(position, rotation);
        playerInstance.transform.localScale = Vector3.one;

        PlatformerPlayerController controller = playerInstance.GetComponent<PlatformerPlayerController>();
        if (controller != null)
        {
            controller.Initialize(LayerMask.GetMask("Default"), new Color(1f, 0.05f, 0.72f), new Color(0.18f, 0.18f, 0.2f), true, true);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = playerInstance;
    }
}
