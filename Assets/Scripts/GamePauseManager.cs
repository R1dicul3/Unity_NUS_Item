using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局游戏暂停管理器。
/// 单例，DontDestroyOnLoad，自动监听 ESC（Menu 动作）以唤出/关闭暂停菜单。
/// 同时负责存档系统的集成（保存、读取、新游戏、返回主菜单）。
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager Instance { get; private set; }

    private PlayerInputActions inputActions;
    private bool isPaused = false;
    private GameObject pauseMenuObject;
    private bool isSaveGameUIOpen = false;
    private SaveSystem.SaveData pendingLoadData;

    public string PreviousGameplayScene { get; private set; }
    public bool CameFromPauseMenu { get; private set; }
    public bool HasUnsavedProgress { get; private set; } = false;

    private readonly string[] menuScenes = { "MainMenu", "LoadGame", "Credits" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GamePauseManager");
            go.AddComponent<GamePauseManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        inputActions = new PlayerInputActions();
        inputActions.Player.Menu.performed += OnMenuPerformed;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (inputActions != null)
        {
            inputActions.Player.Menu.performed -= OnMenuPerformed;
            inputActions.Dispose();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        if (IsInMenuScene())
            return;

        // 如果保存界面或确认对话框打开，不处理 ESC（由它们自己处理）
        if (FindFirstObjectByType<MainMenu.SaveGameUI>() != null)
            return;
        if (FindFirstObjectByType<MainMenu.ConfirmDialogUI>() != null)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private bool IsInMenuScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        foreach (string menuScene in menuScenes)
        {
            if (currentScene == menuScene)
                return true;
        }
        return false;
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuObject == null)
        {
            pauseMenuObject = new GameObject("PauseMenu");
            pauseMenuObject.AddComponent<MainMenu.PauseMenuUI>();
        }
        else
        {
            pauseMenuObject.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }
    }

    public void LoadGame()
    {
        PreviousGameplayScene = SceneManager.GetActiveScene().name;
        CameFromPauseMenu = true;
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }
        SceneManager.LoadScene("LoadGame", LoadSceneMode.Additive);
    }

    public void SaveGame()
    {
        // 旧的无参数 SaveGame 不再使用，改为打开保存界面
        OpenSaveGameUI();
    }

    public void OpenSaveGameUI()
    {
        if (isSaveGameUIOpen)
            return;

        isSaveGameUIOpen = true;
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(false);
        }

        GameObject go = new GameObject("SaveGameUI");
        go.AddComponent<MainMenu.SaveGameUI>();
    }

    public void OnSaveGameUIClosed()
    {
        isSaveGameUIOpen = false;
        if (pauseMenuObject != null)
        {
            pauseMenuObject.SetActive(true);
        }
    }

    public void MarkProgressSaved()
    {
        HasUnsavedProgress = false;
    }

    public void StartNewGame()
    {
        HasUnsavedProgress = true;
        SaveSystem.GameTimer.Instance?.ResetTimer();
        SaveSystem.GameTimer.Instance?.StartTimer();
        SceneManager.LoadScene("Scene_2");
    }

    public void LoadSaveGame(int slot)
    {
        SaveSystem.SaveData data = SaveSystem.SaveSystem.Load(slot);
        if (data == null)
        {
            Debug.LogWarning($"[GamePauseManager] 无法读取存档槽 {slot}。");
            return;
        }

        // 如果从暂停菜单叠加加载了 LoadGame 场景，先卸载它
        if (CameFromPauseMenu)
        {
            SceneManager.UnloadSceneAsync("LoadGame");
            CameFromPauseMenu = false;
        }

        // 重置暂停状态，防止加载后游戏冻结
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenuObject = null;

        pendingLoadData = data;
        HasUnsavedProgress = false;
        SaveSystem.GameTimer.Instance?.SetElapsedTime(data.playTimeSeconds);
        SaveSystem.GameTimer.Instance?.StartTimer();
        SceneManager.LoadScene(data.sceneName);
    }

    public void ReturnToMainMenu()
    {
        if (HasUnsavedProgress)
        {
            MainMenu.ConfirmDialogUI.Show(
                "返回主菜单将丢失未保存的游戏进度，确定吗？",
                onConfirm: () => DoReturnToMainMenu(),
                onCancel: null);
        }
        else
        {
            DoReturnToMainMenu();
        }
    }

    private void DoReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        HasUnsavedProgress = false;
        CameFromPauseMenu = false;
        PreviousGameplayScene = null;
        pauseMenuObject = null;
        SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToGameFromLoadGame()
    {
        CameFromPauseMenu = false;
        SceneManager.UnloadSceneAsync("LoadGame");
        PauseGame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData != null && !IsInMenuScene())
        {
            StartCoroutine(ApplySaveNextFrame(pendingLoadData));
            pendingLoadData = null;
        }
    }

    private IEnumerator ApplySaveNextFrame(SaveSystem.SaveData data)
    {
        // 等待一帧，确保 PlatformerPrototypeBootstrap 已完成初始化
        yield return null;

        var player = FindFirstObjectByType<PlatformerPlayerController>();
        if (player != null)
        {
            player.transform.position = data.playerPosition.ToVector3();
            // 重置速度，防止加载后保留旧动量
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            Debug.LogWarning("[GamePauseManager] 加载存档后未找到玩家对象。");
        }

        // 强制相机立即刷新到玩家位置
        var cameraFollow = FindFirstObjectByType<CameraFollow2D>();
        if (cameraFollow != null)
        {
            cameraFollow.ForceSnapToTarget();
        }

        Debug.Log("[GamePauseManager] 存档数据已应用到场景。");
    }
}
