using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MainMenu.Editor
{
    /// <summary>
    /// 编辑器菜单工具：一键创建 MainMenu、LoadGame、Credits 三个场景，
    /// 并自动添加到 Build Settings 中。
    ///
    /// 使用方式：
    /// 1. 确保本脚本已放入 Assets/Editor 文件夹并在 Unity 中编译通过。
    /// 2. 点击顶部菜单栏 MainMenu > Create All Menu Scenes。
    /// 3. 按提示保存后即可在 Scenes 文件夹下看到三个新场景。
    /// </summary>
    public class MainMenuSceneSetup
    {
        [MenuItem("MainMenu/Create All Menu Scenes")]
        static void CreateAllScenes()
        {
            // 确保目录存在
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scripts/MainMenu");

            Font defaultFont = LoadDefaultFont();

            // MainMenu 场景
            var mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mainGO = new GameObject("MainMenuUI");
            var mainUI = mainGO.AddComponent<MainMenu.MainMenuUI>();
            if (defaultFont != null) mainUI.overrideFont = defaultFont;
            EditorSceneManager.SaveScene(mainScene, "Assets/Scenes/MainMenu.unity");

            // LoadGame 场景
            var loadScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var loadGO = new GameObject("LoadGameUI");
            var loadUI = loadGO.AddComponent<MainMenu.LoadGameUI>();
            if (defaultFont != null) loadUI.overrideFont = defaultFont;
            EditorSceneManager.SaveScene(loadScene, "Assets/Scenes/LoadGame.unity");

            // Credits 场景
            var creditsScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var creditsGO = new GameObject("CreditsUI");
            var creditsUI = creditsGO.AddComponent<MainMenu.CreditsUI>();
            if (defaultFont != null) creditsUI.overrideFont = defaultFont;
            EditorSceneManager.SaveScene(creditsScene, "Assets/Scenes/Credits.unity");

            // 添加到 Build Settings
            AddSceneToBuildSettings("Assets/Scenes/MainMenu.unity");
            AddSceneToBuildSettings("Assets/Scenes/LoadGame.unity");
            AddSceneToBuildSettings("Assets/Scenes/Credits.unity");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("创建完成",
                "已成功创建以下场景并添加到 Build Settings：\n\n" +
                "• Assets/Scenes/MainMenu.unity\n" +
                "• Assets/Scenes/LoadGame.unity\n" +
                "• Assets/Scenes/Credits.unity\n\n" +
                "建议操作流程：\n" +
                "1. 双击打开 MainMenu 场景\n" +
                "2. 点击 Play 即可预览主菜单界面。", "确定");
        }

        [MenuItem("MainMenu/Create MainMenu Scene Only")]
        static void CreateMainMenuScene()
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("MainMenuUI");
            go.AddComponent<MainMenu.MainMenuUI>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            AddSceneToBuildSettings("Assets/Scenes/MainMenu.unity");
            AssetDatabase.Refresh();
        }

        [MenuItem("MainMenu/Create LoadGame Scene Only")]
        static void CreateLoadGameScene()
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("LoadGameUI");
            go.AddComponent<MainMenu.LoadGameUI>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LoadGame.unity");
            AddSceneToBuildSettings("Assets/Scenes/LoadGame.unity");
            AssetDatabase.Refresh();
        }

        [MenuItem("MainMenu/Create Credits Scene Only")]
        static void CreateCreditsScene()
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("CreditsUI");
            go.AddComponent<MainMenu.CreditsUI>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Credits.unity");
            AddSceneToBuildSettings("Assets/Scenes/Credits.unity");
            AssetDatabase.Refresh();
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == path) return; // 已存在
            }

            System.Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = scenes;
        }

        /// <summary>
        /// 尝试加载项目中的默认字体（Assets/Resources/Fonts/Roboto-Regular）。
        /// </summary>
        static Font LoadDefaultFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Roboto-Regular.ttf");
            if (font != null) return font;

            // 尝试查找项目中其他字体
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets" });
            if (fontGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                return AssetDatabase.LoadAssetAtPath<Font>(path);
            }

            return null;
        }
    }
}
