using System.Collections;
using UnityEngine;

/// <summary>
/// 全局音频管理器。单例，DontDestroyOnLoad，自动在场景加载前创建。
/// 负责管理背景音乐（BGM）和音效（SFX）的播放，并与 GameSettings 的音量设置联动。
///
/// 使用方式：
///   AudioManager.Instance?.PlayOneShot(SoundType.Jump);
///   AudioManager.Instance?.PlayMusic(SoundType.GameplayMusic);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Library")]
    [Tooltip("声音资源库。如果留空，会尝试从 Resources/Audio/SoundLibrary 自动加载。")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Pool Size")]
    [Tooltip("音效音频源的对象池大小。同时播放的音效数量超过此值时，最早的音效会被复用。")]
    [SerializeField] private int sfxPoolSize = 8;

    private AudioSource _musicSource;
    private AudioSource[] _sfxSources;
    private int _sfxIndex;

    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
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

        EnsureSoundLibrary();
        EnsureAudioSources();
    }

    private void Start()
    {
        SubscribeToSettings();
        ApplyVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromSettings();
    }

    #region Volume & Settings

    private void SubscribeToSettings()
    {
        if (GameSettings.Instance == null) return;

        GameSettings.Instance.OnMasterVolumeChanged += OnMasterVolumeChanged;
        GameSettings.Instance.OnMusicVolumeChanged += OnMusicVolumeChanged;
        GameSettings.Instance.OnSFXVolumeChanged += OnSFXVolumeChanged;
    }

    private void UnsubscribeFromSettings()
    {
        if (GameSettings.Instance == null) return;

        GameSettings.Instance.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        GameSettings.Instance.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        GameSettings.Instance.OnSFXVolumeChanged -= OnSFXVolumeChanged;
    }

    private void OnMasterVolumeChanged(float value)
    {
        _masterVolume = value;
        ApplyVolume();
    }

    private void OnMusicVolumeChanged(float value)
    {
        _musicVolume = value;
        ApplyMusicVolume();
    }

    private void OnSFXVolumeChanged(float value)
    {
        _sfxVolume = value;
    }

    private void ApplyVolume()
    {
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (_musicSource != null)
        {
            _musicSource.volume = _masterVolume * _musicVolume;
        }
    }

    #endregion

    #region Setup

    private void EnsureSoundLibrary()
    {
        if (soundLibrary != null) return;

        soundLibrary = Resources.Load<SoundLibrary>("Audio/SoundLibrary");
#if UNITY_EDITOR
        if (soundLibrary == null)
        {
            Debug.LogWarning("[AudioManager] 未找到 SoundLibrary 资源。请在 Inspector 中赋值，或在 Assets/Resources/Audio/ 下创建 SoundLibrary.asset。");
        }
#endif
    }

    private void EnsureAudioSources()
    {
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
        }

        if (_sfxSources == null || _sfxSources.Length == 0)
        {
            _sfxSources = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _sfxSources[i] = source;
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 播放指定类型的音效（OneShot）。支持重叠播放。
    /// 如果该 SoundType 在 SoundLibrary 中未配置，调用将被静默忽略。
    /// </summary>
    public void PlayOneShot(SoundType type)
    {
        if (soundLibrary == null) return;
        AudioClip clip = soundLibrary.GetClip(type);
        if (clip == null) return;

        AudioSource source = GetNextSFXSource();
        float volume = _masterVolume * _sfxVolume;
        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// 播放背景音乐。如果已有音乐正在播放，默认会交叉淡入淡出切换。
    /// </summary>
    /// <param name="type">音乐类型</param>
    /// <param name="loop">是否循环</param>
    /// <param name="fadeDuration">淡入淡出时长（秒）。设为 0 则直接切换。</param>
    public void PlayMusic(SoundType type, bool loop = true, float fadeDuration = 0.5f)
    {
        if (soundLibrary == null) return;
        AudioClip clip = soundLibrary.GetClip(type);
        if (clip == null) return;

        if (fadeDuration > 0f && _musicSource.isPlaying)
        {
            StopCoroutine(nameof(CrossfadeMusic));
            StartCoroutine(CrossfadeMusic(clip, loop, fadeDuration));
        }
        else
        {
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();
        }
    }

    /// <summary>
    /// 停止背景音乐。
    /// </summary>
    /// <param name="fadeDuration">淡出时长（秒）。设为 0 则立即停止。</param>
    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (fadeDuration > 0f && _musicSource.isPlaying)
        {
            StopCoroutine(nameof(FadeOutMusic));
            StartCoroutine(FadeOutMusic(fadeDuration));
        }
        else
        {
            _musicSource.Stop();
        }
    }

    /// <summary>
    /// 暂停背景音乐。
    /// </summary>
    public void PauseMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            _musicSource.Pause();
        }
    }

    /// <summary>
    /// 恢复背景音乐。
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicSource != null)
        {
            _musicSource.UnPause();
        }
    }

    #endregion

    #region Internal Helpers

    private AudioSource GetNextSFXSource()
    {
        AudioSource source = _sfxSources[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % _sfxSources.Length;
        return source;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, bool loop, float duration)
    {
        float startVolume = _musicSource.volume;
        float timer = 0f;

        // 淡出
        while (timer < duration * 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration * 0.5f));
            yield return null;
        }

        _musicSource.Stop();
        _musicSource.clip = newClip;
        _musicSource.loop = loop;
        _musicSource.Play();

        float targetVolume = _masterVolume * _musicVolume;
        timer = 0f;

        // 淡入
        while (timer < duration * 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(0f, targetVolume, timer / (duration * 0.5f));
            yield return null;
        }

        _musicSource.volume = targetVolume;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = _musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        _musicSource.Stop();
        _musicSource.volume = _masterVolume * _musicVolume;
    }

    #endregion
}
