using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 声音资源库。通过 CreateAssetMenu 在项目中创建实例，
/// 并在 Inspector 中为每种 SoundType 配置对应的 AudioClip。
/// 建议放置路径：Assets/Resources/Audio/SoundLibrary.asset
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public SoundType type;
        public AudioClip clip;
    }

    [Tooltip("音效与音乐条目列表。每种 SoundType 仅需配置一次。")]
    public SoundEntry[] sounds;

    private Dictionary<SoundType, AudioClip> _lookup;

    /// <summary>
    /// 根据 SoundType 获取对应的 AudioClip。未配置时返回 null。
    /// </summary>
    public AudioClip GetClip(SoundType type)
    {
        if (type == SoundType.None) return null;
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(type, out AudioClip clip);
        return clip;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<SoundType, AudioClip>();
        if (sounds == null) return;

        foreach (SoundEntry entry in sounds)
        {
            if (entry.clip == null) continue;
            _lookup[entry.type] = entry.clip;
        }
    }

    private void OnEnable()
    {
        _lookup = null;
    }
}
