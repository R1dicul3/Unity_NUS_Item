using UnityEngine;

public class Room1FloorSpikes : MonoBehaviour
{
    [Header("Messages")]
    [SerializeField] private string failMessage = "You stepped on spikes! Try again?";

    private bool isDialogShowing;

    private void Awake()
    {
        failMessage = "You stepped on spikes! Try again?";
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDialogShowing)
        {
            return;
        }

        PlatformerPlayerController player = collision.collider.GetComponentInParent<PlatformerPlayerController>();
        if (player == null)
        {
            return;
        }

        isDialogShowing = true;
        Time.timeScale = 0f;

        MainMenu.ConfirmDialogUI.Show(
            failMessage,
            onConfirm: OnRetry,
            onCancel: OnRetry);
    }

    private void OnRetry()
    {
        Time.timeScale = 1f;
        isDialogShowing = false;

        RoomPillarPuzzle2D puzzle = FindFirstObjectByType<RoomPillarPuzzle2D>();
        if (puzzle != null)
        {
            puzzle.RestartPuzzle();
        }
    }
}
