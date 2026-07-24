using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour {

    [SerializeField] private bool requireInteraction = true;
    [SerializeField] private Transform targetSpawn;
    [SerializeField] private Vector2 exitVelocity;
    [SerializeField] private bool preserveHorizontalDirection = true;
    [SerializeField] private bool autoLinkWhenTargetMissing = true;
    [SerializeField] private float alignmentTolerance = 3f;
    [SerializeField] private float maxAutoLinkDistance = 45f;
    [SerializeField] private float exitOffset = 1.25f;
    [SerializeField] private float teleportCooldown = 0.25f;
    [SerializeField] private float promptDisplayDuration = 2f;

    [SerializeField] private CameraArea targetCameraArea;

    [Header("Camera Transition")]
    [Tooltip("Camera transition duration after entering this door. Set to 0 for an instant camera snap.")]
    [SerializeField] private float cameraTransitionDuration = 0.35f;

    [Tooltip("Camera transition easing curve. Leave empty to use the default smooth easing.")]
    [SerializeField]
    private AnimationCurve cameraTransitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio")]
    [Tooltip("BGM to play after entering this door. Set to None to keep current music.")]
    [SerializeField] private SoundType transitionMusic = SoundType.None;

    private static float nextAllowedTeleportTime;

    private bool isPlayerInZone;
    private PlatformerPlayerController currentPlayer;
    private PlayerInputActions inputActions;

    private Coroutine hidePromptCoroutine;
    private float promptShownTime;

    private void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake() {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable() {
        inputActions.Enable();
    }

    private void OnDisable() {
        inputActions.Disable();

        if (hidePromptCoroutine != null) {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        if (isPlayerInZone) {
            InteractPromptController.Instance?.Hide();
        }

        isPlayerInZone = false;
        currentPlayer = null;
    }

    private void OnDestroy() {
        inputActions.Dispose();
    }

    private void Update() {
        if (!requireInteraction ||
            !isPlayerInZone ||
            currentPlayer == null) {
            return;
        }

        if (inputActions.Player.Interact.WasPressedThisFrame()) {
            TryTeleportPlayer(currentPlayer);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player == null) {
            return;
        }

        if (requireInteraction) {
            isPlayerInZone = true;
            currentPlayer = player;
            promptShownTime = Time.time;

            if (hidePromptCoroutine != null) {
                StopCoroutine(hidePromptCoroutine);
                hidePromptCoroutine = null;
            }

            InteractPromptController.Instance?.Show(transform);
        }
        else {
            TryTeleportPlayer(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!requireInteraction) {
            return;
        }

        PlatformerPlayerController player =
            other.GetComponentInParent<PlatformerPlayerController>();

        if (player != null && player == currentPlayer) {
            isPlayerInZone = false;
            currentPlayer = null;

            float elapsed = Time.time - promptShownTime;
            float remaining = promptDisplayDuration - elapsed;

            SchedulePromptHide(remaining);
        }
    }

    private void TryTeleportPlayer(
        PlatformerPlayerController player
    ) {
        if (Time.time < nextAllowedTeleportTime) {
            return;
        }

        Vector3 destination;
        RoomDoor targetDoor;

        if (targetSpawn != null) {
            destination =
                GetTargetSpawnDestination(out targetDoor);
        }
        else if (
            autoLinkWhenTargetMissing &&
            TryGetAutoLinkedDestination(
                out destination,
                out targetDoor
            )
        ) {
        }
        else {
            return;
        }

        Rigidbody2D playerBody =
            player.GetComponent<Rigidbody2D>();

        player.transform.position = destination;

        nextAllowedTeleportTime =
            Time.time + teleportCooldown;

        AudioManager.Instance?.PlayOneShot(SoundType.DoorOpen);

        if (playerBody != null) {
            float horizontalVelocity =
                preserveHorizontalDirection
                    ? Mathf.Sign(
                        playerBody.linearVelocity.x
                    ) *
                    Mathf.Abs(exitVelocity.x)
                    : exitVelocity.x;

            playerBody.linearVelocity =
                new Vector2(
                    horizontalVelocity,
                    exitVelocity.y
                );
        }

        isPlayerInZone = false;
        currentPlayer = null;

        // Pass the active player through so the camera remains bound to the character that used the door.
        UpdateCamera(player);

        SchedulePromptHide(promptDisplayDuration);

        if (transitionMusic != SoundType.None) {
            AudioManager.Instance?.PlayMusic(transitionMusic);
        }
    }

    private void UpdateCamera(PlatformerPlayerController player) {
        PixelPerfectFollowCamera camera =
            FindFirstObjectByType<PixelPerfectFollowCamera>();

        if (camera == null) {
            return;
        }

        // Always bind the camera to the player that entered the door.
        if (player != null) {
            camera.SetTarget(player.transform);
        }

        if (targetCameraArea == null) {
            return;
        }

        camera.TransitionTo(
            targetCameraArea.CameraBounds,
            targetCameraArea.CameraSize,
            cameraTransitionDuration,
            cameraTransitionCurve
        );
    }

    private void SchedulePromptHide(float delay) {
        if (!gameObject.activeInHierarchy) {
            InteractPromptController.Instance?.Hide();
            return;
        }

        if (hidePromptCoroutine != null) {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        if (delay <= 0f) {
            InteractPromptController.Instance?.Hide();
        }
        else {
            hidePromptCoroutine =
                StartCoroutine(
                    HidePromptAfterDelay(delay)
                );
        }
    }

    private IEnumerator HidePromptAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        InteractPromptController.Instance?.Hide();

        hidePromptCoroutine = null;
    }

    private bool TryGetAutoLinkedDestination(
        out Vector3 destination,
        out RoomDoor linkedDoor
    ) {
        destination = Vector3.zero;
        linkedDoor = FindNearestAlignedDoor();

        if (linkedDoor == null) {
            return false;
        }

        Vector2 travelDirection =
            linkedDoor.transform.position -
            transform.position;

        Vector2 exitDirection =
            GetDominantAxisDirection(
                travelDirection
            );

        destination =
            linkedDoor.transform.position +
            (Vector3)(
                exitDirection * exitOffset
            );

        return true;
    }

    private Vector3 GetTargetSpawnDestination(
        out RoomDoor targetDoor
    ) {
        targetDoor =
            targetSpawn.GetComponent<RoomDoor>();

        if (targetDoor == null) {
            return targetSpawn.position;
        }

        Vector2 travelDirection =
            targetDoor.transform.position -
            transform.position;

        Vector2 exitDirection =
            GetDominantAxisDirection(
                travelDirection
            );

        return targetDoor.transform.position +
               (Vector3)(
                   exitDirection * exitOffset
               );
    }

    private RoomDoor FindNearestAlignedDoor() {
        RoomDoor[] doors =
            FindObjectsByType<RoomDoor>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        RoomDoor nearestDoor = null;
        float nearestDistance = float.MaxValue;

        Vector2 currentPosition =
            transform.position;

        foreach (RoomDoor door in doors) {
            if (door == this ||
                !door.isActiveAndEnabled) {
                continue;
            }

            Vector2 delta =
                (Vector2)door.transform.position -
                currentPosition;

            float distance =
                delta.magnitude;

            if (distance <= 0.01f ||
                distance > maxAutoLinkDistance) {
                continue;
            }

            bool horizontallyAligned =
                Mathf.Abs(delta.y) <=
                alignmentTolerance;

            bool verticallyAligned =
                Mathf.Abs(delta.x) <=
                alignmentTolerance;

            if (!horizontallyAligned &&
                !verticallyAligned) {
                continue;
            }

            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearestDoor = door;
            }
        }

        return nearestDoor;
    }

    private static Vector2 GetDominantAxisDirection(
        Vector2 direction
    ) {
        if (direction.sqrMagnitude <= 0.001f) {
            return Vector2.right;
        }

        if (Mathf.Abs(direction.x) >=
            Mathf.Abs(direction.y)) {
            return direction.x >= 0f
                ? Vector2.right
                : Vector2.left;
        }

        return direction.y >= 0f
            ? Vector2.up
            : Vector2.down;
    }
}
