using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldScaleAuditWindow {
    private const float RecommendedPpu = 16f;
    private const float ReferencePlayerHeight = 4.5f;
    private const float MaxGameplayScale = 20f;
    private const float NonUniformRatioWarning = 3f;

    [MenuItem("Tools/World Scale/Audit Open Scenes")]
    public static void AuditOpenScenes() {
        int warningCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects()) {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true)) {
                    warningCount += AuditTransformScale(scene.path, GetHierarchyPath(transform), transform.localScale);

                    GridLayout grid = transform.GetComponent<GridLayout>();
                    if (grid != null) {
                        warningCount += AuditGrid(scene.path, GetHierarchyPath(transform), grid.cellSize);
                    }

                    CameraArea cameraArea = transform.GetComponent<CameraArea>();
                    if (cameraArea != null) {
                        warningCount += AuditCameraArea(scene.path, GetHierarchyPath(transform), cameraArea.CameraSize);
                    }
                }
            }
        }

        Debug.Log($"[World Scale Audit] Open scene audit completed. Warnings: {warningCount}");
    }

    [MenuItem("Tools/World Scale/Audit Project Assets")]
    public static void AuditProjectAssets() {
        int warningCount = 0;
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

        foreach (string guid in textureGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite) continue;

            float ppu = importer.spritePixelsPerUnit;
            if (!Approximately(ppu, RecommendedPpu)) {
                warningCount++;
                Debug.LogWarning($"[World Scale Audit] Sprite PPU exception: {path} uses {ppu.ToString(CultureInfo.InvariantCulture)} PPU. Baseline is {RecommendedPpu} PPU.");
            }
        }

        Debug.Log($"[World Scale Audit] Project asset audit completed. Warnings: {warningCount}");
    }

    [MenuItem("Tools/World Scale/Audit Scene YAML")]
    public static void AuditSceneYaml() {
        int warningCount = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        foreach (string guid in sceneGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            warningCount += AuditSceneFile(path);
        }

        Debug.Log($"[World Scale Audit] Scene YAML audit completed. Warnings: {warningCount}");
    }

    [MenuItem("Tools/World Scale/Analyze Selected CameraArea")]
    public static void AnalyzeSelectedCameraArea() {
        CameraArea area = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<CameraArea>()
            : null;

        if (area == null) {
            Debug.LogWarning("[World Scale Audit] Select a GameObject with CameraArea before running this analysis.");
            return;
        }

        Bounds bounds = area.CameraBounds;
        string scenePath = area.gameObject.scene.path;
        int rendererCount = 0;
        int colliderCount = 0;
        int scaleWarnings = 0;
        int highRiskCount = 0;
        int mediumRiskCount = 0;
        int lowerRiskCount = 0;
        List<string> riskyObjects = new List<string>();
        List<string> gameplayObjects = new List<string>();

        foreach (GameObject root in area.gameObject.scene.GetRootGameObjects()) {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true)) {
                if (!IsInside2D(bounds, transform.position)) continue;

                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null) rendererCount++;

                Collider2D collider = transform.GetComponent<Collider2D>();
                if (collider != null) colliderCount++;

                ObjectRisk risk = ClassifyObjectRisk(transform);
                if (risk == ObjectRisk.High) {
                    highRiskCount++;
                    gameplayObjects.Add($"HIGH   {GetHierarchyPath(transform)}");
                }
                else if (risk == ObjectRisk.Medium) {
                    mediumRiskCount++;
                    gameplayObjects.Add($"MEDIUM {GetHierarchyPath(transform)}");
                }
                else {
                    lowerRiskCount++;
                }

                if (IsScaleOutlier(transform.localScale)) {
                    scaleWarnings++;
                    riskyObjects.Add($"{GetHierarchyPath(transform)} scale={Format(transform.localScale)}");
                }
            }
        }

        PlatformerPlayerController player = Object.FindFirstObjectByType<PlatformerPlayerController>();
        string playerSummary = player != null
            ? $"playerPosition={Format(player.transform.position)} playerInArea={IsInside2D(bounds, player.transform.position)}"
            : "player not found";

        Debug.Log(
            "[World Scale Audit] Selected CameraArea analysis\n"
            + $"Scene: {scenePath}\n"
            + $"Area: {GetHierarchyPath(area.transform)}\n"
            + $"Bounds center={Format(bounds.center)} size={Format(bounds.size)} cameraSize={area.CameraSize.ToString(CultureInfo.InvariantCulture)} preset={GetCameraPresetName(area.CameraSize)} playerScreenHeight={GetReferencePlayerScreenHeightPercent(area.CameraSize)}%\n"
            + $"Objects in area: renderers={rendererCount}, colliders={colliderCount}, scaleWarnings={scaleWarnings}\n"
            + $"Risk classes: high={highRiskCount}, medium={mediumRiskCount}, lower={lowerRiskCount}\n"
            + $"Player: {playerSummary}"
        );

        foreach (string gameplayObject in gameplayObjects) {
            Debug.Log($"[World Scale Audit] Selected area gameplay-sensitive object: {gameplayObject}");
        }

        foreach (string riskyObject in riskyObjects) {
            Debug.LogWarning($"[World Scale Audit] Selected area scale outlier: {riskyObject}");
        }
    }

    private enum ObjectRisk {
        Lower,
        Medium,
        High
    }

    private static int AuditSceneFile(string assetPath) {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath)) return 0;

        int warnings = 0;
        string[] lines = File.ReadAllLines(fullPath);

        Regex scaleRegex = new Regex(@"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
        Regex gridRegex = new Regex(@"m_CellSize: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
        Regex cameraSizeRegex = new Regex(@"cameraSize: ([^\s]+)");

        for (int i = 0; i < lines.Length; i++) {
            Match scaleMatch = scaleRegex.Match(lines[i]);
            if (scaleMatch.Success) {
                Vector3 scale = new Vector3(ParseFloat(scaleMatch.Groups[1].Value), ParseFloat(scaleMatch.Groups[2].Value), ParseFloat(scaleMatch.Groups[3].Value));
                warnings += AuditTransformScale(assetPath, $"line {i + 1}", scale);
                continue;
            }

            Match gridMatch = gridRegex.Match(lines[i]);
            if (gridMatch.Success) {
                Vector3 cellSize = new Vector3(ParseFloat(gridMatch.Groups[1].Value), ParseFloat(gridMatch.Groups[2].Value), ParseFloat(gridMatch.Groups[3].Value));
                warnings += AuditGrid(assetPath, $"line {i + 1}", cellSize);
                continue;
            }

            Match cameraSizeMatch = cameraSizeRegex.Match(lines[i]);
            if (cameraSizeMatch.Success) {
                warnings += AuditCameraArea(assetPath, $"line {i + 1}", ParseFloat(cameraSizeMatch.Groups[1].Value));
            }
        }

        return warnings;
    }

    private static int AuditTransformScale(string scenePath, string objectPath, Vector3 scale) {
        if (Approximately(scale.x, 0f) || Approximately(scale.y, 0f)) {
            Debug.LogWarning($"[World Scale Audit] Zero scale: {scenePath} / {objectPath} scale={Format(scale)}");
            return 1;
        }

        if (IsScaleOutlier(scale)) {
            Debug.LogWarning($"[World Scale Audit] Scale outlier: {scenePath} / {objectPath} scale={Format(scale)}");
            return 1;
        }

        return 0;
    }

    private static bool IsScaleOutlier(Vector3 scale) {
        if (Approximately(scale.x, 0f) || Approximately(scale.y, 0f)) {
            return true;
        }

        float maxAbs = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float minAbs = Mathf.Max(0.0001f, Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));

        return maxAbs > MaxGameplayScale || maxAbs / minAbs > NonUniformRatioWarning;
    }

    private static int AuditGrid(string scenePath, string objectPath, Vector3 cellSize) {
        bool knownGrid = Approximately(cellSize.x, 1f) && Approximately(cellSize.y, 1f)
            || Approximately(cellSize.x, 4f) && Approximately(cellSize.y, 1f)
            || Approximately(cellSize.x, 0.25f) && Approximately(cellSize.y, 0.25f);

        if (!knownGrid) {
            Debug.LogWarning($"[World Scale Audit] Unusual Grid cell size: {scenePath} / {objectPath} cellSize={Format(cellSize)}");
            return 1;
        }

        if (!Approximately(cellSize.x, 1f) || !Approximately(cellSize.y, 1f)) {
            Debug.LogWarning($"[World Scale Audit] Grid uses non-baseline cell size: {scenePath} / {objectPath} cellSize={Format(cellSize)}");
            return 1;
        }

        return 0;
    }

    private static int AuditCameraArea(string scenePath, string objectPath, float cameraSize) {
        if (cameraSize < 4.5f || cameraSize > 13f) {
            Debug.LogWarning($"[World Scale Audit] CameraArea size outside current preset range: {scenePath} / {objectPath} cameraSize={cameraSize.ToString(CultureInfo.InvariantCulture)}");
            return 1;
        }

        return 0;
    }

    private static string GetHierarchyPath(Transform transform) {
        List<string> names = new List<string>();
        Transform current = transform;
        while (current != null) {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names.ToArray());
    }

    private static float ParseFloat(string value) {
        float result;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0f;
    }

    private static bool Approximately(float a, float b) {
        return Mathf.Abs(a - b) < 0.0001f;
    }

    private static bool IsInside2D(Bounds bounds, Vector3 position) {
        return position.x >= bounds.min.x
            && position.x <= bounds.max.x
            && position.y >= bounds.min.y
            && position.y <= bounds.max.y;
    }

    private static ObjectRisk ClassifyObjectRisk(Transform transform) {
        string name = transform.name.ToLowerInvariant();

        if (transform.GetComponent<PlatformerPlayerController>() != null
            || transform.GetComponent<RoomDoor>() != null
            || transform.GetComponent<SceneTransitionDoor>() != null
            || transform.GetComponent<TutorialMessageTrigger>() != null
            || transform.GetComponent<CameraArea>() != null
            || name.Contains("airwall")
            || name.Contains("door")
            || name.Contains("spawn")
            || name.Contains("tutorial")
            || name.Contains("spike")
            || name.Contains("puzzle")) {
            return ObjectRisk.High;
        }

        Collider2D collider = transform.GetComponent<Collider2D>();
        MonoBehaviour[] behaviours = transform.GetComponents<MonoBehaviour>();
        if (collider != null || behaviours.Length > 0) {
            return ObjectRisk.Medium;
        }

        return ObjectRisk.Lower;
    }

    private static string GetCameraPresetName(float cameraSize) {
        if (cameraSize >= 4.5f && cameraSize <= 6f) {
            return "Close";
        }

        if (cameraSize >= 8f && cameraSize <= 10f) {
            return "Standard";
        }

        if (cameraSize >= 11f && cameraSize <= 13f) {
            return "Wide";
        }

        return "BetweenPresets";
    }

    private static string GetReferencePlayerScreenHeightPercent(float cameraSize) {
        if (cameraSize <= 0f) {
            return "n/a";
        }

        float percent = ReferencePlayerHeight / (cameraSize * 2f) * 100f;
        return percent.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string Format(Vector3 value) {
        return $"({value.x.ToString(CultureInfo.InvariantCulture)}, {value.y.ToString(CultureInfo.InvariantCulture)}, {value.z.ToString(CultureInfo.InvariantCulture)})";
    }
}
