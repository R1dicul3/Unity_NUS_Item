using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTransitionDoor : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Scene_2";
    [SerializeField] private float promptDisplayDuration = 2f;
    [SerializeField] private float teleportCooldown = 0.5f;

    [Header("Audio")]
    [Tooltip("BGM to play before switching scenes. Set to None to keep the current music and let the target scene take over if needed.")]
    [SerializeField] private SoundType transitionMusic = SoundType.None;

    private PlayerInputActions inputActions;
    private bool isPlayerInZone;
    private PlatformerPlayerController currentPlayer;
    private Coroutine hidePromptCoroutine;
    private float promptShownTime;
    private float nextAllowedTeleportTime;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();

        if (hidePromptCoroutine != null)
        {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        if (isPlayerInZone)
        {
            InteractPromptController.Instance?.Hide();
        }

        isPlayerInZone = false;
        currentPlayer = null;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (!isPlayerInZone || currentPlayer == null)
            return;

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            LoadTargetScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player == null)
            return;

        isPlayerInZone = true;
        currentPlayer = player;
        promptShownTime = Time.time;

        if (hidePromptCoroutine != null)
        {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        InteractPromptController.Instance?.Show(transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player != null && player == currentPlayer)
        {
            isPlayerInZone = false;
            currentPlayer = null;

            float elapsed = Time.time - promptShownTime;
            float remaining = promptDisplayDuration - elapsed;

            SchedulePromptHide(remaining);
        }
    }

    private void LoadTargetScene()
    {
        if (Time.time < nextAllowedTeleportTime)
            return;

        nextAllowedTeleportTime = Time.time + teleportCooldown;

        InteractPromptController.Instance?.Hide();
        isPlayerInZone = false;
        currentPlayer = null;

        AudioManager.Instance?.PlayOneShot(SoundType.DoorOpen);

        if (transitionMusic != SoundType.None)
        {
            AudioManager.Instance?.PlayMusic(transitionMusic);
        }

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void SchedulePromptHide(float delay)
    {
        if (hidePromptCoroutine != null)
        {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        if (delay <= 0f)
        {
            InteractPromptController.Instance?.Hide();
        }
        else
        {
            hidePromptCoroutine = StartCoroutine(HidePromptAfterDelay(delay));
        }
    }

    private IEnumerator HidePromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        InteractPromptController.Instance?.Hide();

        hidePromptCoroutine = null;
    }
}
