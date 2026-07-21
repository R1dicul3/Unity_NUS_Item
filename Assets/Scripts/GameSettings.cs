using UnityEngine;

/// <summary>
/// 全局游戏设置管理器。
/// 单例，DontDestroyOnLoad，自动在场景加载前创建。
/// 管理音量、CRT 效果强度等玩家偏好设置，使用 PlayerPrefs 持久化。
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    private const string PREF_MASTER_VOLUME = "GameSettings_MasterVolume";
    private const string PREF_MUSIC_VOLUME = "GameSettings_MusicVolume";
    private const string PREF_SFX_VOLUME = "GameSettings_SFXVolume";
    private const string PREF_CRT_INTENSITY = "GameSettings_CRTIntensity";

    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;
    private float _crtIntensity = 0.5f;

    /// <summary>
    /// 总音量（0–1）。当前不实际控制音频，仅持久化数值。
    /// </summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, _masterVolume);
            PlayerPrefs.Save();
            OnMasterVolumeChanged?.Invoke(_masterVolume);
            OnAnySettingChanged?.Invoke();
        }
    }

    /// <summary>
    /// 音乐音量（0–1）。当前不实际控制音频，仅持久化数值。
    /// </summary>
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, _musicVolume);
            PlayerPrefs.Save();
            OnMusicVolumeChanged?.Invoke(_musicVolume);
            OnAnySettingChanged?.Invoke();
        }
    }

    /// <summary>
    /// 音效音量（0–1）。当前不实际控制音频，仅持久化数值。
    /// </summary>
    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _sfxVolume);
            PlayerPrefs.Save();
            OnSFXVolumeChanged?.Invoke(_sfxVolume);
            OnAnySettingChanged?.Invoke();
        }
    }

    /// <summary>
    /// CRT 效果强度（0–1，默认 0.5）。
    /// </summary>
    public float CRTIntensity
    {
        get => _crtIntensity;
        set
        {
            _crtIntensity = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_CRT_INTENSITY, _crtIntensity);
            PlayerPrefs.Save();
            OnCRTIntensityChanged?.Invoke(_crtIntensity);
            OnAnySettingChanged?.Invoke();
        }
    }

    // 事件
    public event System.Action<float> OnMasterVolumeChanged;
    public event System.Action<float> OnMusicVolumeChanged;
    public event System.Action<float> OnSFXVolumeChanged;
    public event System.Action<float> OnCRTIntensityChanged;
    public event System.Action OnAnySettingChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("GameSettings");
        go.AddComponent<GameSettings>();
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

        LoadSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 从 PlayerPrefs 加载所有设置。如果 key 不存在，使用默认值。
    /// </summary>
    private void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        _crtIntensity = PlayerPrefs.GetFloat(PREF_CRT_INTENSITY, 0.5f);
    }

    /// <summary>
    /// 重置所有设置为默认值。
    /// </summary>
    public void ResetToDefaults()
    {
        MasterVolume = 1f;
        MusicVolume = 1f;
        SFXVolume = 1f;
        CRTIntensity = 0.5f;
    }
}
