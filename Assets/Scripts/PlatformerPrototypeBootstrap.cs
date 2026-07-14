using UnityEngine;

public class PlatformerPrototypeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePrototype()
    {
        if (FindFirstObjectByType<PlatformerPlayerController>() != null)
        {
            return;
        }

        PlatformerPrototypeBootstrap bootstrap = new GameObject("Platformer Prototype Bootstrap").AddComponent<PlatformerPrototypeBootstrap>();
        bootstrap.Build();
    }

    private void Build()
    {
        LayerMask groundMask = LayerMask.GetMask("Default");
        CreateBackground();
        CreateMap();

        PlatformerPlayerController player = CreatePlayer("Player", new Vector2(-7f, -0.25f), groundMask, new Color(1f, 0.82f, 0.32f), true, false);

        Camera camera = ConfigureCamera(player.transform);
        CreateCharacterSwitcher(player);
        CreateInstructionHud(camera.transform);
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
        renderer.color = new Color(1f, 0.82f, 0.32f);
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
        GameObject switcherObject = new GameObject("Character Ability Switcher");
        CharacterSwitcher2D switcher = switcherObject.AddComponent<CharacterSwitcher2D>();
        switcher.Initialize(player);
    }

    private static void CreateMap()
    {
        CreatePlatform("Ground", new Vector2(0f, -2.8f), new Vector2(22f, 1f), new Color(0.18f, 0.2f, 0.24f));
        CreatePlatform("Start Platform", new Vector2(-6.7f, -1.35f), new Vector2(3.4f, 0.45f), new Color(0.23f, 0.42f, 0.48f));
        CreatePlatform("Step Platform 1", new Vector2(-2.4f, -0.5f), new Vector2(2.7f, 0.4f), new Color(0.28f, 0.52f, 0.47f));
        CreatePlatform("Step Platform 2", new Vector2(1.5f, 0.6f), new Vector2(2.6f, 0.4f), new Color(0.45f, 0.54f, 0.35f));
        CreatePlatform("Dash Gap Platform", new Vector2(6f, 0.2f), new Vector2(2.7f, 0.4f), new Color(0.48f, 0.37f, 0.56f));
        CreatePlatform("High Double Jump Platform", new Vector2(9.2f, 1.8f), new Vector2(2.4f, 0.4f), new Color(0.56f, 0.42f, 0.34f));
        CreatePlatform("Goal Platform", new Vector2(13f, 0.95f), new Vector2(3.2f, 0.45f), new Color(0.32f, 0.5f, 0.7f));

        CreateHazardPit(new Vector2(3.75f, -2.2f), new Vector2(2.4f, 0.18f));
        CreateFlag(new Vector2(14.3f, 1.75f));
    }

    private static void CreatePlatform(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject platform = new GameObject(name);
        platform.transform.position = position;

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = color;
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static void CreateHazardPit(Vector2 position, Vector2 size)
    {
        GameObject pit = new GameObject("Dash Practice Gap Marker");
        pit.transform.position = position;
        pit.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = pit.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.9f, 0.25f, 0.25f, 0.9f);
    }

    private static void CreateFlag(Vector2 position)
    {
        GameObject pole = new GameObject("Goal Flag Pole");
        pole.transform.position = position + Vector2.up * 0.55f;
        pole.transform.localScale = new Vector3(0.12f, 1.3f, 1f);
        SpriteRenderer poleRenderer = pole.AddComponent<SpriteRenderer>();
        poleRenderer.sprite = CreateSquareSprite();
        poleRenderer.color = new Color(0.92f, 0.92f, 0.86f);

        GameObject flag = new GameObject("Goal Flag");
        flag.transform.position = position + new Vector2(0.42f, 1f);
        flag.transform.localScale = new Vector3(0.8f, 0.45f, 1f);
        SpriteRenderer flagRenderer = flag.AddComponent<SpriteRenderer>();
        flagRenderer.sprite = CreateSquareSprite();
        flagRenderer.color = new Color(0.18f, 0.78f, 0.46f);
    }

    private static void CreateBackground()
    {
        GameObject sky = new GameObject("Sky Backdrop");
        sky.transform.position = new Vector3(3f, 0.8f, 8f);
        sky.transform.localScale = new Vector3(28f, 12f, 1f);
        SpriteRenderer renderer = sky.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.08f, 0.12f, 0.17f);
        renderer.sortingOrder = -10;

        for (int i = 0; i < 8; i++)
        {
            GameObject light = new GameObject("Background Light " + i);
            light.transform.position = new Vector3(-8f + i * 3.2f, 2.4f + Mathf.Sin(i * 1.7f) * 1.1f, 7f);
            light.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
            SpriteRenderer lightRenderer = light.AddComponent<SpriteRenderer>();
            lightRenderer.sprite = CreateSquareSprite();
            lightRenderer.color = new Color(0.98f, 0.87f, 0.52f, 0.65f);
            lightRenderer.sortingOrder = -9;
        }
    }

    private static Camera ConfigureCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.17f);
        camera.transform.position = new Vector3(-5f, 0f, -10f);

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
        text.text = "Z: Switch Ability   Yellow: Double Jump   Blue: Dash   A/D: Move   Space: Jump   Left Shift: Dash";
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
