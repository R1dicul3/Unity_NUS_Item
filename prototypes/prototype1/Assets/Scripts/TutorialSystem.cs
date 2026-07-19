using UnityEngine;

public class TutorialSystem : MonoBehaviour
{
    public enum TutorialPhase
    {
        Move,
        Jump,
        DoubleJump,
        Dash,
        Complete
    }

    [SerializeField] private PlatformerPlayerController player;
    [SerializeField] private Transform cameraTransform;

    private TutorialPhase currentPhase = TutorialPhase.Move;
    private float startX;
    private int prevJumpsUsed;
    private bool wasDashing;
    private TextMesh hudText;
    private bool isRunning = true;

    public TutorialPhase CurrentPhase => currentPhase;
    public System.Action onTutorialComplete;

    public void Initialize(PlatformerPlayerController targetPlayer, Transform camTransform)
    {
        player = targetPlayer;
        cameraTransform = camTransform;
        startX = player.transform.position.x;
        prevJumpsUsed = player.JumpsUsed;
        wasDashing = player.IsDashing;

        CreateHud();
        UpdateHudText();
    }

    private void Update()
    {
        if (!isRunning || player == null)
        {
            return;
        }

        int currentJumpsUsed = player.JumpsUsed;
        bool isDashing = player.IsDashing;

        switch (currentPhase)
        {
            case TutorialPhase.Move:
                if (Mathf.Abs(player.transform.position.x - startX) > 1.0f)
                {
                    AdvancePhase();
                }
                break;

            case TutorialPhase.Jump:
                if (prevJumpsUsed == 0 && currentJumpsUsed == 1)
                {
                    AdvancePhase();
                }
                break;

            case TutorialPhase.DoubleJump:
                if (prevJumpsUsed == 1 && currentJumpsUsed == 2)
                {
                    AdvancePhase();
                }
                break;

            case TutorialPhase.Dash:
                if (!wasDashing && isDashing)
                {
                    AdvancePhase();
                }
                break;

            case TutorialPhase.Complete:
                isRunning = false;
                onTutorialComplete?.Invoke();
                break;
        }

        prevJumpsUsed = currentJumpsUsed;
        wasDashing = isDashing;
    }

    private void AdvancePhase()
    {
        currentPhase++;
        UpdateHudText();
    }

    private void UpdateHudText()
    {
        if (hudText == null)
        {
            return;
        }

        switch (currentPhase)
        {
            case TutorialPhase.Move:
                hudText.text = "按 A/D 或 ←/→ 移动";
                break;
            case TutorialPhase.Jump:
                hudText.text = "按 Space 跳跃";
                break;
            case TutorialPhase.DoubleJump:
                hudText.text = "在空中再次按 Space 进行二段跳";
                break;
            case TutorialPhase.Dash:
                hudText.text = "按 Left Shift 冲刺";
                break;
            case TutorialPhase.Complete:
                hudText.text = "前往旗子进入下一个房间";
                break;
        }
    }

    public void SetHudText(string text)
    {
        if (hudText != null)
        {
            hudText.text = text;
        }
    }

    private void CreateHud()
    {
        GameObject hud = new GameObject("Tutorial HUD");
        hud.transform.SetParent(cameraTransform, false);
        hudText = hud.AddComponent<TextMesh>();
        hudText.fontSize = 36;
        hudText.characterSize = 0.08f;
        hudText.anchor = TextAnchor.UpperCenter;
        hudText.color = new Color(0.88f, 0.94f, 1f);
        hud.transform.localPosition = new Vector3(0f, 3.5f, 9f);
    }
}
