#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor 工具：一键生成 Ending 场景。
/// </summary>
public static class CreateEndingScene
{
    [MenuItem("Tools/UI/Create Ending Scene")]
    public static void Generate()
    {
        // 创建新场景
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 创建 EndingUI 物体
        GameObject endingUIGO = new GameObject("EndingUI");
        endingUIGO.AddComponent<MainMenu.EndingUI>();

        // 保存场景
        string scenePath = "Assets/Scenes/Core/Ending.unity";
        string directory = System.IO.Path.GetDirectoryName(scenePath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        EditorSceneManager.SaveScene(scene, scenePath);

        EditorUtility.DisplayDialog("Done", $"Ending scene created at:\n{scenePath}", "OK");
    }
}
#endif
