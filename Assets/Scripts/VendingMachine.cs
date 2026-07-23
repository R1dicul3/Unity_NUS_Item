using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class CoffeeInteractable : MonoBehaviour {
    [Header("Coffee Generation Settings")]
    [Tooltip("放入3种咖啡的预制体 (Prefabs)")]
    [SerializeField] private GameObject[] coffeePrefabs = new GameObject[3];

    [Tooltip("咖啡生成的具体位置")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("指定要生成的咖啡索引 (0 = 第一种, 1 = 第二种, 2 = 第三种)")]
    [Range(0, 2)]
    [SerializeField] private int specifiedCoffeeIndex = 0;

    [Tooltip("是否在3种咖啡中随机生成一种？(勾选后上面的指定索引将失效)")]
    [SerializeField] private bool spawnRandomInstead = false;

    [Header("Interaction Settings")]
    [Tooltip("是否只能交互一次？(勾选后生成完咖啡就不再响应)")]
    [SerializeField] private bool interactOnlyOnce = true;

    [Tooltip("UI提示文字最少显示的时长（秒），即便玩家快速离开也会停留这么多时间")]
    [SerializeField] private float promptDisplayDuration = 2f;

    private bool isPlayerInZone;
    private PlatformerPlayerController currentPlayer;
    private PlayerInputActions inputActions;

    private Coroutine hidePromptCoroutine;
    private float promptShownTime;
    private bool hasInteracted;

    // 当脚本挂载到物体上时自动勾选 isTrigger
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

        // 禁用时清理协程和UI，防止报错或残留
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
        if (!isPlayerInZone || currentPlayer == null) {
            return;
        }

        if (interactOnlyOnce && hasInteracted) {
            return;
        }

        // 采用与 RoomDoor 相同的 InputSystem 按键检测
        if (inputActions.Player.Interact.WasPressedThisFrame()) {
            Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // 与门逻辑一致，通过获取玩家控制器来确认身份
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();

        if (player == null) {
            return;
        }

        // 如果设置了只能交互一次且已经交互过了，就不再显示提示
        if (interactOnlyOnce && hasInteracted) {
            return;
        }

        isPlayerInZone = true;
        currentPlayer = player;
        promptShownTime = Time.time; // 记录UI显示的开始时间

        if (hidePromptCoroutine != null) {
            StopCoroutine(hidePromptCoroutine);
            hidePromptCoroutine = null;
        }

        InteractPromptController.Instance?.Show(transform);
    }

    private void OnTriggerExit2D(Collider2D other) {
        PlatformerPlayerController player = other.GetComponentInParent<PlatformerPlayerController>();

        if (player != null && player == currentPlayer) {
            isPlayerInZone = false;
            currentPlayer = null;

            // 计算已经显示了多久，算出还需要停留的时间
            float elapsed = Time.time - promptShownTime;
            float remaining = promptDisplayDuration - elapsed;

            SchedulePromptHide(remaining);
        }
    }

    private void Interact() {
        hasInteracted = true;

        SpawnCoffee();

        // 如果是一次性交互，生成完咖啡立即隐藏 UI
        if (interactOnlyOnce) {
            SchedulePromptHide(0f);
        }
    }

    private void SpawnCoffee() {
        if (coffeePrefabs == null || coffeePrefabs.Length == 0) {
            return;
        }

        // 决定索引
        int indexToSpawn = specifiedCoffeeIndex;
        if (spawnRandomInstead) {
            indexToSpawn = Random.Range(0, coffeePrefabs.Length);
        }

        // 安全限制
        indexToSpawn = Mathf.Clamp(indexToSpawn, 0, coffeePrefabs.Length - 1);
        GameObject selectedCoffeePrefab = coffeePrefabs[indexToSpawn];

        if (selectedCoffeePrefab != null) {
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            Instantiate(selectedCoffeePrefab, spawnPos, Quaternion.identity);

            // AudioManager.Instance?.PlayOneShot(SoundType.DoorOpen); // 如果需要播放音效可以取消注释并修改 SoundType
        }
    }

    // ---------- UI 隐藏协程逻辑 (与 RoomDoor 一致) ----------

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
            hidePromptCoroutine = StartCoroutine(HidePromptAfterDelay(delay));
        }
    }

    private IEnumerator HidePromptAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        InteractPromptController.Instance?.Hide();
        hidePromptCoroutine = null;
    }
}