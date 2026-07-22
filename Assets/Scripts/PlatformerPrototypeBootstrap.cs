using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformerPrototypeBootstrap : MonoBehaviour
{
    private const string PrototypeScenePathMarker = "/Prototypes/";
    private static readonly bool ConfigureExistingSceneObjects = false;
    private static readonly bool CreateFallbackDialogueModule = false;
    private static readonly bool CreateFallbackInstructionHud = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePrototype()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.path.Contains(PrototypeScenePathMarker))
        {
            return;
        }

        PlatformerPrototypeBootstrap bootstrap = new GameObject("Platformer Prototype Bootstrap").AddComponent<PlatformerPrototypeBootstrap>();
        bootstrap.Build();
    }

    private void Build()
    {
        LayerMask groundMask = LayerMask.GetMask("Default");
        Vector2 spawnPosition = Vector2.zero;
        Color playerColor = new Color(1f, 0.05f, 0.72f);
        PlatformerPlayerController player = FindFirstObjectByType<PlatformerPlayerController>(FindObjectsInactive.Include);
        if (player == null)
        {
            player = CreatePlayer("Player", spawnPosition, groundMask, playerColor, true, true);
        }
        else if (ConfigureExistingSceneObjects)
        {
            player.Initialize(groundMask, playerColor, new Color(0.18f, 0.18f, 0.2f), true, true);
        }

        Camera camera = ConfigureCamera(player.transform);
        CreateCharacterSwitcher(player);
        CreateDialogueModule();
        if (CreateFallbackInstructionHud)
        {
            CreateInstructionHud(camera.transform);
        }
    }

    private static PlatformerPlayerController CreatePlayer(string name, Vector2 position, LayerMask groundMask, Color activeColor, bool canDoubleJump, bool canDash)
    {
        GameObject player = new GameObject(name);
        player.transform.position = position;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.2f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 1.05f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = activeColor;
        visual.transform.localScale = new Vector3(0.75f, 1.05f, 1f);

        TrailRenderer trail = player.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.55f;
        trail.endWidth = 0f;
        trail.emitting = false;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.25f, 0.88f, 1f, 0.7f);
        trail.endColor = new Color(0.25f, 0.88f, 1f, 0f);

        PlatformerPlayerController controller = player.AddComponent<PlatformerPlayerController>();
        controller.Initialize(groundMask, activeColor, new Color(activeColor.r * 0.45f, activeColor.g * 0.45f, activeColor.b * 0.45f, 1f), canDoubleJump, canDash);

        return controller;
    }

    private static void CreateCharacterSwitcher(PlatformerPlayerController player)
    {
        CharacterSwitcher2D switcher = FindFirstObjectByType<CharacterSwitcher2D>();
        if (switcher == null)
        {
            GameObject switcherObject = new GameObject("Character Ability Switcher");
            switcher = switcherObject.AddComponent<CharacterSwitcher2D>();
        }

        switcher.Initialize(player);
    }

    private static void CreateDialogueModule()
    {
        DialogueController dialogue = FindFirstObjectByType<DialogueController>();
        if (dialogue == null)
        {
            if (!CreateFallbackDialogueModule)
            {
                return;
            }

            GameObject dialogueObject = new GameObject("Dialogue Module");
            dialogue = dialogueObject.AddComponent<DialogueController>();
            dialogue.SetLines(new[]
            {
                new DialogueController.DialogueLine { speaker = "Left", speakerSide = DialogueController.SpeakerSide.Left, text = "The left portrait is active while this character is speaking." },
                new DialogueController.DialogueLine { speaker = "Right", speakerSide = DialogueController.SpeakerSide.Right, text = "Now the right portrait lights up and the left portrait dims." },
                new DialogueController.DialogueLine { speaker = "Left", speakerSide = DialogueController.SpeakerSide.Left, text = "Replace these lines from code or the Inspector for reusable conversations." }
            });
        }
    }

    private static Camera ConfigureCamera(Transform player)
    {
        Camera camera = Camera.main;
        bool createdCamera = camera == null;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        if (createdCamera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.17f);
            camera.transform.position = new Vector3(player.position.x, player.position.y, -10f);
        }

        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<CameraFollow2D>();
        }

        follow.SetTarget(player);
        return camera;
    }

    private static void CreateInstructionHud(Transform cameraTransform)
    {
        GameObject hud = new GameObject("Prototype Controls HUD");
        hud.transform.SetParent(cameraTransform, false);
        TextMesh text = hud.AddComponent<TextMesh>();
        text.text = "Z: Switch Ability   Magenta: Double Jump + Dash   Green: Single Jump Only   A/D: Move   Space: Jump   Left Shift: Dash";
        text.fontSize = 36;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.UpperLeft;
        text.color = new Color(0.88f, 0.94f, 1f);
        hud.transform.localPosition = new Vector3(-7.35f, 3.95f, 9f);
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
