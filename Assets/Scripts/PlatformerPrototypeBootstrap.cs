using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformerPrototypeBootstrap : MonoBehaviour
{
    private const string PrototypeScenePathMarker = "/Prototypes/";

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

        Camera camera = ConfigureCamera(player.transform);
        CreateCharacterSwitcher(player);
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

        PixelPerfectFollowCamera follow = camera.GetComponent<PixelPerfectFollowCamera>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<PixelPerfectFollowCamera>();
        }

        follow.SetTarget(player);
        return camera;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
