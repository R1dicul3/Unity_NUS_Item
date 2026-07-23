using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ManWalkAnimationBuilder
{
    private const string SourceSheetPath = "Assets/Art/Characters/Man/Man_walk_transparent.png";
    private const string AnimationFolder = "Assets/Animations/Characters/Man";
    private const string ResourceAnimationFolder = "Assets/Resources/Animations";
    private const string IdleClipPath = AnimationFolder + "/Man_Idle.anim";
    private const string WalkClipPath = AnimationFolder + "/Man_Walk.anim";
    private const string ControllerPath = ResourceAnimationFolder + "/ManBasic.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string AutoRunFlagPath = "Assets/Editor/RunManWalkAnimationBuild.flag";
    private const int Columns = 4;
    private const int Rows = 3;
    private const int FrameCount = 12;
    private const float WalkFrameRate = 12f;

    [InitializeOnLoadMethod]
    private static void BuildFromFlagWhenEditorReloads()
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
            Build();
        };
    }

    [MenuItem("Tools/Animation/Build Man Walk For Green Character")]
    public static void Build()
    {
        if (!File.Exists(SourceSheetPath))
        {
            Debug.LogError($"Missing source sprite sheet: {SourceSheetPath}");
            return;
        }

        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Characters");
        EnsureFolder("Assets/Animations/Characters", "Man");
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Animations");

        ConfigureSpriteSheet();
        Sprite[] frames = LoadFrames();
        if (frames.Length != FrameCount)
        {
            Debug.LogError($"Expected {FrameCount} Man walk sprites, found {frames.Length}.");
            return;
        }

        AnimationClip idle = CreateSpriteClip(IdleClipPath, "Man_Idle", new[] { frames[0] }, false);
        AnimationClip walk = CreateSpriteClip(WalkClipPath, "Man_Walk", frames.Skip(1).ToArray(), true);
        AnimatorController controller = CreateController(idle, walk);

        UpdatePlayerPrefab(controller, frames[0]);
        UpdateOpenSceneSwitchers(controller, frames[0]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Man walk animation built and assigned to the green/basic character mode.");
    }

    private static void ConfigureSpriteSheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SourceSheetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Unable to load TextureImporter for {SourceSheetPath}");
            return;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceSheetPath);
        int width = texture != null ? texture.width : 2048;
        int height = texture != null ? texture.height : 2048;
        int frameHeight = Mathf.RoundToInt(height / (float)Rows);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = frameHeight;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        List<SpriteMetaData> sprites = new List<SpriteMetaData>();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int index = row * Columns + col;
                int xMin = Mathf.RoundToInt(col * width / (float)Columns);
                int xMax = Mathf.RoundToInt((col + 1) * width / (float)Columns);
                int yMin = height - Mathf.RoundToInt((row + 1) * height / (float)Rows);
                int yMax = height - Mathf.RoundToInt(row * height / (float)Rows);

                sprites.Add(new SpriteMetaData
                {
                    name = $"Man_walk_transparent_{index}",
                    rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f)
                });
            }
        }

        importer.spritesheet = sprites.ToArray();
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadFrames()
    {
        return AssetDatabase.LoadAllAssetsAtPath(SourceSheetPath)
            .OfType<Sprite>()
            .OrderBy(GetFrameIndex)
            .ToArray();
    }

    private static int GetFrameIndex(Sprite sprite)
    {
        int separatorIndex = sprite.name.LastIndexOf('_');
        if (separatorIndex >= 0 && int.TryParse(sprite.name.Substring(separatorIndex + 1), out int index))
        {
            return index;
        }

        return int.MaxValue;
    }

    private static AnimationClip CreateSpriteClip(string path, string clipName, Sprite[] frames, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName, frameRate = WalkFrameRate };
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            clip.ClearCurves();
            clip.name = clipName;
            clip.frameRate = WalkFrameRate;
        }

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / WalkFrameRate,
                value = frames[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new[]
        {
            new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name = "IsGrounded", type = AnimatorControllerParameterType.Bool },
            new AnimatorControllerParameter { name = "VerticalVelocity", type = AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name = "IsDashing", type = AnimatorControllerParameterType.Bool }
        };

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorControllerLayer layer = controller.layers[0];
        layer.defaultWeight = 1f;
        controller.layers = new[] { layer };

        foreach (ChildAnimatorState state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idle;
        stateMachine.defaultState = idleState;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walk;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.05f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.05f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void UpdatePlayerPrefab(RuntimeAnimatorController controller, Sprite idleSprite)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            PlatformerPlayerController player = prefabRoot.GetComponent<PlatformerPlayerController>();
            SpriteRenderer renderer = prefabRoot.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
            {
                renderer.sprite = idleSprite;
                renderer.color = Color.white;

                Animator animator = renderer.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = renderer.gameObject.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
            }

            if (player != null)
            {
                SerializedObject serializedPlayer = new SerializedObject(player);
                SetVector3IfPresent(serializedPlayer, "visualLocalScale", Vector3.one);
                serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void UpdateOpenSceneSwitchers(RuntimeAnimatorController controller, Sprite idleSprite)
    {
        foreach (CharacterSwitcher2D switcher in Object.FindObjectsByType<CharacterSwitcher2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serializedSwitcher = new SerializedObject(switcher);
            SetObjectIfPresent(serializedSwitcher, "basicAnimationPlaceholder", controller);

            Animator animator = switcher.GetComponentInChildren<Animator>(true);
            PlatformerPlayerController player = Object.FindFirstObjectByType<PlatformerPlayerController>(FindObjectsInactive.Include);
            if (animator == null && player != null)
            {
                SpriteRenderer renderer = player.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null)
                {
                    animator = renderer.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = renderer.gameObject.AddComponent<Animator>();
                    }
                }
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.SetLayerWeight(0, 1f);
                SetObjectIfPresent(serializedSwitcher, "characterAnimator", animator);

                SpriteRenderer renderer = animator.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = idleSprite;
                    renderer.color = Color.white;
                }
            }

            serializedSwitcher.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(switcher);
        }

        EditorSceneManager.MarkAllScenesDirty();
    }

    private static void SetObjectIfPresent(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetVector3IfPresent(SerializedObject serializedObject, string propertyName, Vector3 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
