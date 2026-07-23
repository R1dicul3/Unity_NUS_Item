using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace MainMenu
{
    public class AddFluidBackgroundToPrefabs : EditorWindow
    {
        [MenuItem("Tools/UI/Add Fluid Background to All Menus")]
        static void AddFluidBackground()
        {
            string[] prefabPaths = new string[]
            {
                "Assets/Prefabs/UI/PauseMenuCanvas.prefab",
                "Assets/Prefabs/UI/LoadGamePanelCanvas.prefab",
                "Assets/Prefabs/UI/SaveGamePanelCanvas.prefab"
            };

            foreach (var path in prefabPaths)
            {
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabAsset == null)
                {
                    Debug.LogWarning($"[FluidSetup] Prefab not found: {path}");
                    continue;
                }

                // Check if already has fluid background
                PixelFluidBackground existing = prefabAsset.GetComponentInChildren<PixelFluidBackground>();
                if (existing != null)
                {
                    Debug.Log($"[FluidSetup] Fluid background already exists in {path}");
                    continue;
                }

                // Load for editing
                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                Canvas canvas = instance.GetComponentInChildren<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning($"[FluidSetup] No Canvas found in {path}");
                    PrefabUtility.UnloadPrefabContents(instance);
                    continue;
                }

                // Create FluidBackground GameObject
                GameObject bgObj = new GameObject("FluidBackground");
                bgObj.transform.SetParent(canvas.transform, false);
                bgObj.transform.SetAsFirstSibling(); // ensure it's behind everything

                // Full-screen RectTransform
                RectTransform rt = bgObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                // RawImage (no raycast blocking)
                RawImage rawImage = bgObj.AddComponent<RawImage>();
                rawImage.raycastTarget = false;
                rawImage.color = Color.white;

                // PixelFluidBackground script
                PixelFluidBackground fluid = bgObj.AddComponent<PixelFluidBackground>();

                // Save prefab
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);

                Debug.Log($"[FluidSetup] Added fluid background to {path}");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "Fluid background added to all menu prefabs!", "OK");
        }
    }
}
