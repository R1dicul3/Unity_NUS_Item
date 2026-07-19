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

    private PlatformerPlayerController player;
    private Camera mainCamera;
    private TutorialSystem tutorialSystem;

    private void Build()
    {
        LayerMask groundMask = LayerMask.GetMask("Default");
        CreateBackground();

        player = CreatePlayer("Player", new Vector2(-7f, -0.25f), groundMask, new Color(1f, 0.82f, 0.32f), true, true);
        mainCamera = ConfigureCamera(player.transform);

        CreateTutorialRoom();
        CreatePracticeRoom();
        SetupTutorialSystem();
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

    private void SetupTutorialSystem()
    {
        GameObject tutorialObj = new GameObject("Tutorial System");
        tutorialSystem = tutorialObj.AddComponent<TutorialSystem>();
        tutorialSystem.Initialize(player, mainCamera.transform);
        tutorialSystem.onTutorialComplete += OnTutorialComplete;
    }

    private void OnTutorialComplete()
    {
        CreateGoalFlag("Room1 Goal Flag", new Vector2(8f, -1.8f), OnRoom1GoalReached);
    }

    private void OnRoom1GoalReached()
    {
        Vector2 room2Start = new Vector2(48f, 0f);
        player.transform.position = room2Start;
        player.SetControlled(true);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        mainCamera.transform.position = new Vector3(room2Start.x, room2Start.y, -10f);

        if (tutorialSystem != null)
        {
            tutorialSystem.SetHudText("使用学到的动作向上到达终点！");
        }
    }

    private void CreateTutorialRoom()
    {
        CreatePlatform("Tutorial Ground", new Vector2(0f, -2.8f), new Vector2(24f, 1f), new Color(0.18f, 0.2f, 0.24f));
    }

    private void CreatePracticeRoom()
    {
        float offsetX = 50f;

        CreatePlatform("Room2 Ground", new Vector2(offsetX + 8f, -2.8f), new Vector2(30f, 1f), new Color(0.18f, 0.2f, 0.24f));
        CreatePlatform("Room2 Start", new Vector2(offsetX - 2f, -1.0f), new Vector2(3f, 0.4f), new Color(0.23f, 0.42f, 0.48f));
        CreatePlatform("Room2 Step 1", new Vector2(offsetX, 1.0f), new Vector2(2.5f, 0.4f), new Color(0.28f, 0.52f, 0.47f));
        CreatePlatform("Room2 Step 2", new Vector2(offsetX + 2f, 3.0f), new Vector2(2.5f, 0.4f), new Color(0.45f, 0.54f, 0.35f));
        CreatePlatform("Room2 DoubleJump Platform", new Vector2(offsetX + 4f, 6.5f), new Vector2(2.0f, 0.4f), new Color(0.56f, 0.42f, 0.34f));
        CreatePlatform("Room2 Dash Start", new Vector2(offsetX + 7f, 8.0f), new Vector2(1.5f, 0.4f), new Color(0.48f, 0.37f, 0.56f));
        CreatePlatform("Room2 Dash End", new Vector2(offsetX + 11f, 8.0f), new Vector2(1.5f, 0.4f), new Color(0.48f, 0.37f, 0.56f));
        CreatePlatform("Room2 Final Platform", new Vector2(offsetX + 19f, 11.5f), new Vector2(2f, 0.4f), new Color(0.32f, 0.5f, 0.7f));

        CreateGoalFlag("Room2 Goal Flag", new Vector2(offsetX + 19f, 12.3f), OnRoom2GoalReached);
    }

    private void CreateGoalFlag(string name, Vector2 position, System.Action callback)
    {
        GameObject flagObj = new GameObject(name);

        GameObject pole = new GameObject("Pole");
        pole.transform.SetParent(flagObj.transform, false);
        pole.transform.localPosition = Vector2.up * 0.55f;
        pole.transform.localScale = new Vector3(0.12f, 1.3f, 1f);
        SpriteRenderer poleRenderer = pole.AddComponent<SpriteRenderer>();
        poleRenderer.sprite = CreateSquareSprite();
        poleRenderer.color = new Color(0.92f, 0.92f, 0.86f);

        GameObject flag = new GameObject("Flag");
        flag.transform.SetParent(flagObj.transform, false);
        flag.transform.localPosition = new Vector2(0.42f, 1f);
        flag.transform.localScale = new Vector3(0.8f, 0.45f, 1f);
        SpriteRenderer flagRenderer = flag.AddComponent<SpriteRenderer>();
        flagRenderer.sprite = CreateSquareSprite();
        flagRenderer.color = new Color(0.18f, 0.78f, 0.46f);

        GoalFlag goal = flagObj.AddComponent<GoalFlag>();
        goal.Initialize(position, callback);
    }

    private void OnRoom2GoalReached()
    {
        player.SetControlled(false);
        if (tutorialSystem != null)
        {
            tutorialSystem.SetHudText("Prototype Complete!");
        }
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

    private void CreateBackground()
    {
        GameObject sky = new GameObject("Sky Backdrop");
        sky.transform.position = new Vector3(25f, 2f, 8f);
        sky.transform.localScale = new Vector3(80f, 18f, 1f);
        SpriteRenderer renderer = sky.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.08f, 0.12f, 0.17f);
        renderer.sortingOrder = -10;

        for (int i = 0; i < 20; i++)
        {
            GameObject light = new GameObject("Background Light " + i);
            light.transform.position = new Vector3(-10f + i * 3.5f, 3f + Mathf.Sin(i * 1.3f) * 1.5f, 7f);
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

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
