using UnityEngine;

/// <summary>
/// 支持的语言类型。
/// </summary>
public enum Language
{
    English,
    Chinese
}

/// <summary>
/// 本地化文本管理器。
/// 提供静态字典查询，所有键必须在 <see cref="LocalizationData"/> 中定义。
/// 语言切换时触发 <see cref="OnLanguageChanged"/> 事件，供 UI 动态刷新。
/// </summary>
public static class LocalizationManager
{
    public static Language CurrentLanguage { get; private set; } = Language.English;

    public static event System.Action OnLanguageChanged;

    /// <summary>
    /// 初始化当前语言（应在 GameSettings 加载后调用一次）。
    /// </summary>
    public static void Initialize(Language language)
    {
        CurrentLanguage = language;
    }

    /// <summary>
    /// 切换语言并触发全局刷新事件。
    /// </summary>
    public static void SetLanguage(Language language)
    {
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// 根据当前语言获取本地化文本。如果 key 不存在，返回 key 本身作为兜底。
    /// </summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        switch (CurrentLanguage)
        {
            case Language.Chinese:
                if (LocalizationData.Chinese.TryGetValue(key, out string cn))
                    return cn;
                break;
            default:
                if (LocalizationData.English.TryGetValue(key, out string en))
                    return en;
                break;
        }

        return key;
    }

    /// <summary>
    /// 带格式化参数的本地化获取。
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}

/// <summary>
/// 本地化文本数据表。所有 UI 使用的文本键集中定义在此，便于统一维护。
/// </summary>
public static class LocalizationData
{
    public static readonly System.Collections.Generic.Dictionary<string, string> English =
        new System.Collections.Generic.Dictionary<string, string>
    {
        // Main Menu
        { "GameTitle", "Stopover" },
        { "NewGame", "New Game" },
        { "LoadGame", "Load Game" },
        { "Settings", "Settings" },
        { "Credits", "Credits" },
        { "Exit", "Exit" },

        // Pause Menu
        { "ResumeGame", "Resume Game" },
        { "SaveGame", "Save Game" },
        { "ReturnToMainMenu", "Return to Main Menu" },

        // Settings
        { "MasterVolume", "Master Volume" },
        { "BGMVolume", "BGM Volume" },
        { "SFXVolume", "SFX Volume" },
        { "Language", "Language" },
        { "Back", "< Back" },
        { "Confirm", "Confirm" },
        { "Cancel", "Cancel" },

        // Load / Save
        { "SelectSave", "Select Save" },
        { "Load", "Load" },
        { "Delete", "Delete" },
        { "SlotEmpty", "(Empty)" },
        { "PlayTime", "Play Time" },
        { "SavedMessage", "Saved." },
        { "DeletedMessage", "Deleted." },
        { "PleaseSelectSlot", "Please select a save slot first." },
        { "SlotEmptyCannotLoad", "Selected slot is empty, cannot load." },
        { "SlotEmptyCannotDelete", "Selected slot is empty, cannot delete." },
        { "DeleteConfirm", "Are you sure you want to delete save slot {0}? This action cannot be undone." },
        { "OverwriteConfirm", "Save slot {0} already has data. Overwrite it?" },
        { "SaveSlotsFull", "Save slots are full. Starting a new game will overwrite a save slot. Continue?" },

        // Credits
        { "CreditsTitle", "Credits" },

        // Misc
        { "FeatureNotImplemented", "[{0}] Feature not yet implemented." },
    };

    public static readonly System.Collections.Generic.Dictionary<string, string> Chinese =
        new System.Collections.Generic.Dictionary<string, string>
    {
        // Main Menu
        { "GameTitle", "Stopover" },
        { "NewGame", "开始游戏" },
        { "LoadGame", "读取存档" },
        { "Settings", "设置" },
        { "Credits", "制作人员" },
        { "Exit", "退出游戏" },

        // Pause Menu
        { "ResumeGame", "继续游戏" },
        { "SaveGame", "保存游戏" },
        { "ReturnToMainMenu", "返回主菜单" },

        // Settings
        { "MasterVolume", "主音量" },
        { "BGMVolume", "音乐音量" },
        { "SFXVolume", "音效音量" },
        { "Language", "语言" },
        { "Back", "< 返回" },
        { "Confirm", "确认" },
        { "Cancel", "取消" },

        // Load / Save
        { "SelectSave", "选择存档" },
        { "Load", "读取" },
        { "Delete", "删除" },
        { "SlotEmpty", "(空)" },
        { "PlayTime", "游戏时长" },
        { "SavedMessage", "已保存。" },
        { "DeletedMessage", "已删除。" },
        { "PleaseSelectSlot", "请先选择一个存档槽位。" },
        { "SlotEmptyCannotLoad", "所选槽位为空，无法读取。" },
        { "SlotEmptyCannotDelete", "所选槽位为空，无法删除。" },
        { "DeleteConfirm", "确定要删除存档槽位 {0} 吗？此操作无法撤销。" },
        { "OverwriteConfirm", "存档槽位 {0} 已有数据，是否覆盖？" },
        { "SaveSlotsFull", "存档槽位已满。开始新游戏将覆盖某个存档槽位，是否继续？" },

        // Credits
        { "CreditsTitle", "制作人员" },

        // Misc
        { "FeatureNotImplemented", "[{0}] 功能尚未实现。" },
    };
}
