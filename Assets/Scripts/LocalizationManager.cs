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

        // Dialogue & Ending & Misc
        { "Dlg_Main_GirlIntro_01_Speaker", "Girl" },
        { "Dlg_Main_GirlIntro_01_Text", "It's cold." },
        { "Dlg_Main_GirlIntro_02_Speaker", "Girl" },
        { "Dlg_Main_GirlIntro_02_Text", "Another day is over, \n...don't like saying that." },
        { "Dlg_Main_Vending_01_Speaker", "Vending Machine" },
        { "Dlg_Main_Vending_01_Text", "Just a normal coffee vending machine." },
        { "Dlg_Main_Room01_01_Speaker", "Room 01" },
        { "Dlg_Main_Room01_01_Text", "Empty Room" },
        { "Dlg_Main_RegCard_01_Speaker", "Reg Card" },
        { "Dlg_Main_RegCard_01_Text", "Room Number: 03\nGuest Name: Smith, John\nCheck-out: NA\n " },
        { "Dlg_Main_RegCard_02_Speaker", "Girl" },
        { "Dlg_Main_RegCard_02_Text", "Still here." },
        { "Dlg_Main_RegCard_03_Speaker", "Girl" },
        { "Dlg_Main_RegCard_03_Text", "I was already here, told him and waited." },
        { "Dlg_Main_RegCard_04_Speaker", "Girl" },
        { "Dlg_Main_RegCard_04_Text", "...He still won't leave." },
        { "Dlg_Main_Room02_01_Speaker", "Room 02" },
        { "Dlg_Main_Room02_01_Text", "Empty Room" },
        { "Dlg_Main_RoadSign_01_Speaker", "Road Sign" },
        { "Dlg_Main_RoadSign_01_Text", "Way to hotel  Black Sabbath." },
        { "Dlg_Main_RoadSign_02_Speaker", "Road Sign" },
        { "Dlg_Main_RoadSign_02_Text", "Press A/D to move\n\nPress Space/A to jump\n\nPress Space/A in\nmid-air to double jump" },
        { "Dlg_Main_RoadSign_03_Speaker", "Road Sign" },
        { "Dlg_Main_RoadSign_03_Text", "You can interact to some objects like this." },
        { "Dlg_Main_TutorialDash_01_Speaker", "Tutorial" },
        { "Dlg_Main_TutorialDash_01_Text", "Press Shift/RT to dash." },
        { "Dlg_S2_TutorialChar_01_Speaker", "Tutorial" },
        { "Dlg_S2_TutorialChar_01_Text", "Press J/LB to change charactors\n\nPress K/RB to move between the\nReal World and the Ghost World\n\n " },
        { "Dlg_S2_TutorialChar_02_Speaker", "Tutorial" },
        { "Dlg_S2_TutorialChar_02_Text", "Only the office worker can move between worlds\n\nThe office work\ncannot double jump or dash\n\n " },
        { "Dlg_S2_Coffee_01_Speaker", "Girl" },
        { "Dlg_S2_Coffee_01_Text", "(Helding the coffee cup)\n\nStill terrible.\n\n " },
        { "Dlg_S2_Coffee_02_Speaker", "Office Worker" },
        { "Dlg_S2_Coffee_02_Text", "And you still drink it?" },
        { "Dlg_S2_Coffee_03_Speaker", "Girl" },
        { "Dlg_S2_Coffee_03_Text", "I'm used to it." },
        { "Dlg_S2_Coffee_04_Speaker", "Office Worker" },
        { "Dlg_S2_Coffee_04_Text", "Do they serve coffee where you 're from?" },
        { "Dlg_S2_Coffee_05_Speaker", "Girl" },
        { "Dlg_S2_Coffee_05_Text", "...Yes." },
        { "Dlg_S2_Coffee_06_Speaker", "Office Worker" },
        { "Dlg_S2_Coffee_06_Text", "Is it better than this?" },
        { "Dlg_S2_Coffee_07_Speaker", "Girl" },
        { "Dlg_S2_Coffee_07_Text", "No." },
        { "Dlg_S2_Coffee_08_Speaker", "Office Worker" },
        { "Dlg_S2_Coffee_08_Text", "Then why do you drink it?" },
        { "Dlg_S2_Coffee_09_Speaker", "Girl" },
        { "Dlg_S2_Coffee_09_Text", "I'm not used to that one yet." },
        { "Dlg_S2_Identity_01_Speaker", "Office Worker" },
        { "Dlg_S2_Identity_01_Text", "Who are you?" },
        { "Dlg_S2_Identity_02_Speaker", "Girl" },
        { "Dlg_S2_Identity_02_Text", "..." },
        { "Dlg_S2_Identity_03_Speaker", "Office Worker" },
        { "Dlg_S2_Identity_03_Text", "You 're not a tour guide." },
        { "Dlg_S2_Identity_04_Speaker", "Girl" },
        { "Dlg_S2_Identity_04_Text", "I never said I was." },
        { "Dlg_S2_Identity_05_Speaker", "Office Worker" },
        { "Dlg_S2_Identity_05_Text", "Then what are you?" },
        { "Dlg_S2_Identity_06_Speaker", "Girl" },
        { "Dlg_S2_Identity_06_Text", "......" },
        { "Dlg_S2_Identity_07_Speaker", "Girl" },
        { "Dlg_S2_Identity_07_Text", "A Reaper." },
        { "Dlg_S2_Identity_08_Speaker", "Office Worker" },
        { "Dlg_S2_Identity_08_Text", "You don't look like one." },
        { "Dlg_S2_Identity_09_Speaker", "Girl" },
        { "Dlg_S2_Identity_09_Text", "...Thank you." },
        { "Dlg_S2_Identity_10_Speaker", "Office Worker" },
        { "Dlg_S2_Identity_10_Text", "Was that a compliment?" },
        { "Dlg_S2_Identity_11_Speaker", "Girl" },
        { "Dlg_S2_Identity_11_Text", "I don't know." },
        { "Dlg_S2_Arrival_01_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_01_Text", "Are you new here?" },
        { "Dlg_S2_Arrival_02_Speaker", "Girl" },
        { "Dlg_S2_Arrival_02_Text", "Something like that." },
        { "Dlg_S2_Arrival_03_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_03_Text", "Which room are you staying in?" },
        { "Dlg_S2_Arrival_04_Speaker", "Girl" },
        { "Dlg_S2_Arrival_04_Text", "Haven't decided yet." },
        { "Dlg_S2_Arrival_05_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_05_Text", "(The office worker repeated yesterday's words, as if nothing had changed)" },
        { "Dlg_S2_Arrival_06_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_06_Text", "What time do I work tomorrow?" },
        { "Dlg_S2_Arrival_07_Speaker", "Girl" },
        { "Dlg_S2_Arrival_07_Text", "You 're dead!!!" },
        { "Dlg_S2_Arrival_08_Speaker", "Girl" },
        { "Dlg_S2_Arrival_08_Text", "......\n\nI'm sorry." },
        { "Dlg_S2_Arrival_09_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_09_Text", "Why are you apologizing?" },
        { "Dlg_S2_Arrival_10_Speaker", "Girl" },
        { "Dlg_S2_Arrival_10_Text", "...Because I'm always too late." },
        { "Dlg_S2_Arrival_11_Speaker", "Office Worker" },
        { "Dlg_S2_Arrival_11_Text", "But you're here now." },
        { "Dlg_S2_Goodbye_01_Speaker", "Office Worker" },
        { "Dlg_S2_Goodbye_01_Text", "See you tomorrow." },
        { "Dlg_S2_Goodbye_02_Speaker", "Girl" },
        { "Dlg_S2_Goodbye_02_Text", "See ya." },
        { "Dlg_S2_Module_01_Speaker", "???" },
        { "Dlg_S2_Module_01_Text", "Are you new here?" },
        { "Dlg_S2_Module_02_Speaker", "Girl" },
        { "Dlg_S2_Module_02_Text", "Something like that." },
        { "Dlg_S2_Module_03_Speaker", "Office Worker" },
        { "Dlg_S2_Module_03_Text", "Which room are you staying in?" },
        { "Dlg_S2_Module_04_Speaker", "Girl" },
        { "Dlg_S2_Module_04_Text", "I haven't decided yet." },
        { "Dlg_S2_Module_05_Speaker", "Office Worker" },
        { "Dlg_S2_Module_05_Text", "What do you do?" },
        { "Dlg_S2_Module_06_Speaker", "Girl" },
        { "Dlg_S2_Module_06_Text", "...I guide people." },
        { "Dlg_S2_Module_07_Speaker", "Office Worker" },
        { "Dlg_S2_Module_07_Text", "A tour guide?" },
        { "Dlg_S2_Module_08_Speaker", "Girl" },
        { "Dlg_S2_Module_08_Text", "Something like that." },
        { "Dlg_S2_Module_09_Speaker", "Girl" },
        { "Dlg_S2_Module_09_Text", "You should go." },
        { "Dlg_S2_Module_10_Speaker", "Office Worker" },
        { "Dlg_S2_Module_10_Text", "Where?" },
        { "Dlg_S2_Module_11_Speaker", "Girl" },
        { "Dlg_S2_Module_11_Text", "......" },
        { "Dlg_S2_Module_12_Speaker", "Office Worker" },
        { "Dlg_S2_Module_12_Text", "Aren't you a guide?" },
        { "Dlg_S2_Module_13_Speaker", "Girl" },
        { "Dlg_S2_Module_13_Text", "...You're dead." },
        { "Dlg_S2_Module_14_Speaker", "Office Worker" },
        { "Dlg_S2_Module_14_Text", "What?" },
        { "Dlg_S2_Module_15_Speaker", "Girl" },
        { "Dlg_S2_Module_15_Text", "You 're dead." },
        { "Dlg_S2_Module_16_Speaker", "Office Worker" },
        { "Dlg_S2_Module_16_Text", "What time do I work tomorrow?" },
        { "Dlg_S2_Module_17_Speaker", "Girl" },
        { "Dlg_S2_Module_17_Text", "...Did you not hear me, then why are you asking about tomorrow?" },
        { "Dlg_S2_Module_18_Speaker", "Office Worker" },
        { "Dlg_S2_Module_18_Text", "Because tomorrow is coming." },
        { "Dlg_S2_Module_19_Speaker", "Girl" },
        { "Dlg_S2_Module_19_Text", "Not necessarily." },
        { "Dlg_S2_Reflection_01_Speaker", "Office Worker" },
        { "Dlg_S2_Reflection_01_Text", "How long have you been doing this?" },
        { "Dlg_S2_Reflection_02_Speaker", "Girl" },
        { "Dlg_S2_Reflection_02_Text", "...A long time" },
        { "Dlg_S2_Reflection_03_Speaker", "Office Worker" },
        { "Dlg_S2_Reflection_03_Text", "Have you guided many people?" },
        { "Dlg_S2_Reflection_04_Speaker", "Girl" },
        { "Dlg_S2_Reflection_04_Text", "Yes,\n\n...but most of them were afraid of me." },
        { "Dlg_S2_Reflection_05_Speaker", "Office Worker" },
        { "Dlg_S2_Reflection_05_Text", "Do you hate that?" },
        { "Dlg_S2_Reflection_06_Speaker", "Girl" },
        { "Dlg_S2_Reflection_06_Text", "I do, they don't see me, they see death.And then they are all afraid." },
        { "Dlg_S2_Reflection_07_Speaker", "Office Worker" },
        { "Dlg_S2_Reflection_07_Text", "Aren't you?" },
        { "Dlg_S2_Reflection_08_Speaker", "Girl" },
        { "Dlg_S2_Reflection_08_Text", "Afraid of what?" },
        { "Dlg_S2_Reflection_09_Speaker", "Office Worker" },
        { "Dlg_S2_Reflection_09_Text", "Of being feared." },
        { "Dlg_S2_Reflection_10_Speaker", "Girl" },
        { "Dlg_S2_Reflection_10_Text", "......" },
        { "Dlg_S2_TutorialCoffee_01_Speaker", "Tutorial" },
        { "Dlg_S2_TutorialCoffee_01_Text", "Only the Office Worker can drink coffee" },
        // Dlg_S2_Ahead (new room dialogue)
        { "Dlg_S2_Ahead_01_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_01_Text", "Do you know what's ahead?" },
        { "Dlg_S2_Ahead_02_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_02_Text", "No." },
        { "Dlg_S2_Ahead_03_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_03_Text", "Aren't you a reaper?" },
        { "Dlg_S2_Ahead_04_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_04_Text", "I only know how to bring people here." },
        { "Dlg_S2_Ahead_05_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_05_Text", "And then." },
        { "Dlg_S2_Ahead_06_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_06_Text", "Then I don't know." },
        { "Dlg_S2_Ahead_07_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_07_Text", "Then why do you keep leading people?" },
        { "Dlg_S2_Ahead_08_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_08_Text", "Because..." },
        { "Dlg_S2_Ahead_09_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_09_Text", "If I don't...\n\n...They' ll have to walk alone." },
        { "Dlg_S2_Ahead_10_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_10_Text", "Then let's go together." },
        { "Dlg_S2_Ahead_11_Speaker", "Reaper" },
        { "Dlg_S2_Ahead_11_Text", "...You're really troublesome." },
        { "Dlg_S2_Ahead_12_Speaker", "Office Worker" },
        { "Dlg_S2_Ahead_12_Text", "I know." },
        { "ReturnConfirmMessage", "Returning to the main menu will lose unsaved progress. Are you sure?" },
        { "Ending_ThankYou", "Thank you for playing." },
        { "Ending_JourneyEnd", "Your journey has come to an end." },
        { "Ending_NewBeginning", "But every ending is a new beginning." },
        { "Ending_SeeYouAgain", "We hope to see you again." },
        { "Ending_SkipPrompt", "Press any key to skip" },
        { "Prompt_Interact", "Press Enter/X" },

        // EndingCanvas Prefab
        { "EndingCanvas_SoAtLeast", "So, at the very least" },
        { "EndingCanvas_SkipPrompt", "Press any key to skip" },
        { "EndingCanvas_ReaperName", "I still don't like the name, \"Reaper\"." },
        { "EndingCanvas_WalkWithThem", "And walk with them through the very last part of the journey." },
        { "EndingCanvas_ICanStay", "I can stay." },
        { "EndingCanvas_HateHelpless", "And I still hate not being able to do anything." },
        { "EndingCanvas_TheEnd", "The End" },
        { "EndingCanvas_CantChange", "There are things I really can't change." },
        { "EndingCanvas_Feared", "I still don't like the way people fear me." },
    };

    public static readonly System.Collections.Generic.Dictionary<string, string> Chinese =
        new System.Collections.Generic.Dictionary<string, string>
    {
        // Main Menu
        { "GameTitle", "Stopover" },
        { "NewGame", "开始游戏" },
        { "LoadGame", "读取存档" },
        { "Settings", "设置" },
        { "Credits", "Credits" },
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
        { "CreditsTitle", "Credits" },

        // Misc
        { "FeatureNotImplemented", "[{0}] 功能尚未实现。" },

        // Dialogue & Ending & Misc
        { "Dlg_Main_GirlIntro_01_Speaker", "女孩" },
        { "Dlg_Main_GirlIntro_01_Text", "好冷。" },
        { "Dlg_Main_GirlIntro_02_Speaker", "女孩" },
        { "Dlg_Main_GirlIntro_02_Text", "又一天结束了，\n……不想这么说。" },
        { "Dlg_Main_Vending_01_Speaker", "自动售货机" },
        { "Dlg_Main_Vending_01_Text", "只是一台普通的咖啡自动售货机。" },
        { "Dlg_Main_Room01_01_Speaker", "01号房间" },
        { "Dlg_Main_Room01_01_Text", "空房间" },
        { "Dlg_Main_RegCard_01_Speaker", "登记卡" },
        { "Dlg_Main_RegCard_01_Text", "房间号：03\n客人姓名：Smith, John\n退房：无\n " },
        { "Dlg_Main_RegCard_02_Speaker", "女孩" },
        { "Dlg_Main_RegCard_02_Text", "还在这里。" },
        { "Dlg_Main_RegCard_03_Speaker", "女孩" },
        { "Dlg_Main_RegCard_03_Text", "我已经在这里了，告诉了他，然后等着。" },
        { "Dlg_Main_RegCard_04_Speaker", "女孩" },
        { "Dlg_Main_RegCard_04_Text", "……他还是不肯离开。" },
        { "Dlg_Main_Room02_01_Speaker", "02号房间" },
        { "Dlg_Main_Room02_01_Text", "空房间" },
        { "Dlg_Main_RoadSign_01_Speaker", "路标" },
        { "Dlg_Main_RoadSign_01_Text", "通往 Black Sabbath 酒店的路。" },
        { "Dlg_Main_RoadSign_02_Speaker", "路标" },
        { "Dlg_Main_RoadSign_02_Text", "按 A/D 移动\n\n按 Space/A 跳跃\n\n在空中按 Space/A 二段跳" },
        { "Dlg_Main_RoadSign_03_Speaker", "路标" },
        { "Dlg_Main_RoadSign_03_Text", "你可以与这样的物体互动。" },
        { "Dlg_Main_TutorialDash_01_Speaker", "教程" },
        { "Dlg_Main_TutorialDash_01_Text", "按 Shift/RT 冲刺。" },
        { "Dlg_S2_TutorialChar_01_Speaker", "教程" },
        { "Dlg_S2_TutorialChar_01_Text", "按 J/LB 切换角色\n\n按 K/RB 在现实世界\n与幽灵世界之间移动\n\n " },
        { "Dlg_S2_TutorialChar_02_Speaker", "教程" },
        { "Dlg_S2_TutorialChar_02_Text", "只有上班族能在世界之间移动\n\n上班族不能\n二段跳或冲刺\n\n " },
        { "Dlg_S2_Coffee_01_Speaker", "女孩" },
        { "Dlg_S2_Coffee_01_Text", "（端着咖啡杯）\n\n还是很难喝。\n\n " },
        { "Dlg_S2_Coffee_02_Speaker", "上班族" },
        { "Dlg_S2_Coffee_02_Text", "那你还在喝？" },
        { "Dlg_S2_Coffee_03_Speaker", "女孩" },
        { "Dlg_S2_Coffee_03_Text", "我习惯了。" },
        { "Dlg_S2_Coffee_04_Speaker", "上班族" },
        { "Dlg_S2_Coffee_04_Text", "你来自的地方也有咖啡吗？" },
        { "Dlg_S2_Coffee_05_Speaker", "女孩" },
        { "Dlg_S2_Coffee_05_Text", "……有。" },
        { "Dlg_S2_Coffee_06_Speaker", "上班族" },
        { "Dlg_S2_Coffee_06_Text", "比这个好喝吗？" },
        { "Dlg_S2_Coffee_07_Speaker", "女孩" },
        { "Dlg_S2_Coffee_07_Text", "不。" },
        { "Dlg_S2_Coffee_08_Speaker", "上班族" },
        { "Dlg_S2_Coffee_08_Text", "那你为什么还要喝？" },
        { "Dlg_S2_Coffee_09_Speaker", "女孩" },
        { "Dlg_S2_Coffee_09_Text", "那个我还没习惯。" },
        { "Dlg_S2_Identity_01_Speaker", "上班族" },
        { "Dlg_S2_Identity_01_Text", "你是谁？" },
        { "Dlg_S2_Identity_02_Speaker", "女孩" },
        { "Dlg_S2_Identity_02_Text", "……" },
        { "Dlg_S2_Identity_03_Speaker", "上班族" },
        { "Dlg_S2_Identity_03_Text", "你不是导游。" },
        { "Dlg_S2_Identity_04_Speaker", "女孩" },
        { "Dlg_S2_Identity_04_Text", "我从没说过我是。" },
        { "Dlg_S2_Identity_05_Speaker", "上班族" },
        { "Dlg_S2_Identity_05_Text", "那你是什么？" },
        { "Dlg_S2_Identity_06_Speaker", "女孩" },
        { "Dlg_S2_Identity_06_Text", "……" },
        { "Dlg_S2_Identity_07_Speaker", "女孩" },
        { "Dlg_S2_Identity_07_Text", "死神。" },
        { "Dlg_S2_Identity_08_Speaker", "上班族" },
        { "Dlg_S2_Identity_08_Text", "你看起来不像。" },
        { "Dlg_S2_Identity_09_Speaker", "女孩" },
        { "Dlg_S2_Identity_09_Text", "……谢谢。" },
        { "Dlg_S2_Identity_10_Speaker", "上班族" },
        { "Dlg_S2_Identity_10_Text", "这是在夸我吗？" },
        { "Dlg_S2_Identity_11_Speaker", "女孩" },
        { "Dlg_S2_Identity_11_Text", "我不知道。" },
        { "Dlg_S2_Arrival_01_Speaker", "上班族" },
        { "Dlg_S2_Arrival_01_Text", "你是新来的吗？" },
        { "Dlg_S2_Arrival_02_Speaker", "女孩" },
        { "Dlg_S2_Arrival_02_Text", "算是吧。" },
        { "Dlg_S2_Arrival_03_Speaker", "上班族" },
        { "Dlg_S2_Arrival_03_Text", "你住哪个房间？" },
        { "Dlg_S2_Arrival_04_Speaker", "女孩" },
        { "Dlg_S2_Arrival_04_Text", "还没决定。" },
        { "Dlg_S2_Arrival_05_Speaker", "上班族" },
        { "Dlg_S2_Arrival_05_Text", "（上班族重复着昨天的话，仿佛一切都没有改变）" },
        { "Dlg_S2_Arrival_06_Speaker", "上班族" },
        { "Dlg_S2_Arrival_06_Text", "我明天几点上班？" },
        { "Dlg_S2_Arrival_07_Speaker", "女孩" },
        { "Dlg_S2_Arrival_07_Text", "你已经死了！！！" },
        { "Dlg_S2_Arrival_08_Speaker", "女孩" },
        { "Dlg_S2_Arrival_08_Text", "……\n\n对不起。" },
        { "Dlg_S2_Arrival_09_Speaker", "上班族" },
        { "Dlg_S2_Arrival_09_Text", "你为什么要道歉？" },
        { "Dlg_S2_Arrival_10_Speaker", "女孩" },
        { "Dlg_S2_Arrival_10_Text", "……因为我总是太晚了。" },
        { "Dlg_S2_Arrival_11_Speaker", "上班族" },
        { "Dlg_S2_Arrival_11_Text", "但你现在在这里。" },
        { "Dlg_S2_Goodbye_01_Speaker", "上班族" },
        { "Dlg_S2_Goodbye_01_Text", "明天见。" },
        { "Dlg_S2_Goodbye_02_Speaker", "女孩" },
        { "Dlg_S2_Goodbye_02_Text", "再见。" },
        { "Dlg_S2_Module_01_Speaker", "???" },
        { "Dlg_S2_Module_01_Text", "你是新来的吗？" },
        { "Dlg_S2_Module_02_Speaker", "女孩" },
        { "Dlg_S2_Module_02_Text", "算是吧。" },
        { "Dlg_S2_Module_03_Speaker", "上班族" },
        { "Dlg_S2_Module_03_Text", "你住哪个房间？" },
        { "Dlg_S2_Module_04_Speaker", "女孩" },
        { "Dlg_S2_Module_04_Text", "我还没决定。" },
        { "Dlg_S2_Module_05_Speaker", "上班族" },
        { "Dlg_S2_Module_05_Text", "你是做什么的？" },
        { "Dlg_S2_Module_06_Speaker", "女孩" },
        { "Dlg_S2_Module_06_Text", "……我引导人们。" },
        { "Dlg_S2_Module_07_Speaker", "上班族" },
        { "Dlg_S2_Module_07_Text", "导游？" },
        { "Dlg_S2_Module_08_Speaker", "女孩" },
        { "Dlg_S2_Module_08_Text", "算是吧。" },
        { "Dlg_S2_Module_09_Speaker", "女孩" },
        { "Dlg_S2_Module_09_Text", "你应该走了。" },
        { "Dlg_S2_Module_10_Speaker", "上班族" },
        { "Dlg_S2_Module_10_Text", "去哪？" },
        { "Dlg_S2_Module_11_Speaker", "女孩" },
        { "Dlg_S2_Module_11_Text", "……" },
        { "Dlg_S2_Module_12_Speaker", "上班族" },
        { "Dlg_S2_Module_12_Text", "你不是导游吗？" },
        { "Dlg_S2_Module_13_Speaker", "女孩" },
        { "Dlg_S2_Module_13_Text", "……你已经死了。" },
        { "Dlg_S2_Module_14_Speaker", "上班族" },
        { "Dlg_S2_Module_14_Text", "什么？" },
        { "Dlg_S2_Module_15_Speaker", "女孩" },
        { "Dlg_S2_Module_15_Text", "你已经死了。" },
        { "Dlg_S2_Module_16_Speaker", "上班族" },
        { "Dlg_S2_Module_16_Text", "我明天几点上班？" },
        { "Dlg_S2_Module_17_Speaker", "女孩" },
        { "Dlg_S2_Module_17_Text", "……你没听见我说的话吗，那为什么还要问明天？" },
        { "Dlg_S2_Module_18_Speaker", "上班族" },
        { "Dlg_S2_Module_18_Text", "因为明天总会来的。" },
        { "Dlg_S2_Module_19_Speaker", "女孩" },
        { "Dlg_S2_Module_19_Text", "不一定。" },
        { "Dlg_S2_Reflection_01_Speaker", "上班族" },
        { "Dlg_S2_Reflection_01_Text", "你做这个多久了？" },
        { "Dlg_S2_Reflection_02_Speaker", "女孩" },
        { "Dlg_S2_Reflection_02_Text", "……很久了" },
        { "Dlg_S2_Reflection_03_Speaker", "上班族" },
        { "Dlg_S2_Reflection_03_Text", "你引导过很多人吗？" },
        { "Dlg_S2_Reflection_04_Speaker", "女孩" },
        { "Dlg_S2_Reflection_04_Text", "是的，\n\n……但他们大多数都怕我。" },
        { "Dlg_S2_Reflection_05_Speaker", "上班族" },
        { "Dlg_S2_Reflection_05_Text", "你讨厌那样吗？" },
        { "Dlg_S2_Reflection_06_Speaker", "女孩" },
        { "Dlg_S2_Reflection_06_Text", "我讨厌，他们看到的不是我，而是死亡。然后他们都害怕了。" },
        { "Dlg_S2_Reflection_07_Speaker", "上班族" },
        { "Dlg_S2_Reflection_07_Text", "你不怕吗？" },
        { "Dlg_S2_Reflection_08_Speaker", "女孩" },
        { "Dlg_S2_Reflection_08_Text", "怕什么？" },
        { "Dlg_S2_Reflection_09_Speaker", "上班族" },
        { "Dlg_S2_Reflection_09_Text", "怕被恐惧。" },
        { "Dlg_S2_Reflection_10_Speaker", "女孩" },
        { "Dlg_S2_Reflection_10_Text", "……" },
        { "Dlg_S2_TutorialCoffee_01_Speaker", "教程" },
        { "Dlg_S2_TutorialCoffee_01_Text", "只有上班族能喝咖啡" },
        // Dlg_S2_Ahead (new room dialogue)
        { "Dlg_S2_Ahead_01_Speaker", "上班族" },
        { "Dlg_S2_Ahead_01_Text", "你知道前面是什么吗？" },
        { "Dlg_S2_Ahead_02_Speaker", "死神" },
        { "Dlg_S2_Ahead_02_Text", "不知道。" },
        { "Dlg_S2_Ahead_03_Speaker", "上班族" },
        { "Dlg_S2_Ahead_03_Text", "你不是死神吗？" },
        { "Dlg_S2_Ahead_04_Speaker", "死神" },
        { "Dlg_S2_Ahead_04_Text", "我只知道怎么把人们带到这里。" },
        { "Dlg_S2_Ahead_05_Speaker", "上班族" },
        { "Dlg_S2_Ahead_05_Text", "然后呢。" },
        { "Dlg_S2_Ahead_06_Speaker", "死神" },
        { "Dlg_S2_Ahead_06_Text", "然后我也不知道。" },
        { "Dlg_S2_Ahead_07_Speaker", "上班族" },
        { "Dlg_S2_Ahead_07_Text", "那你为什么一直引导人们？" },
        { "Dlg_S2_Ahead_08_Speaker", "死神" },
        { "Dlg_S2_Ahead_08_Text", "因为..." },
        { "Dlg_S2_Ahead_09_Speaker", "死神" },
        { "Dlg_S2_Ahead_09_Text", "如果我不这样做……\n\n……他们就只能独自前行。" },
        { "Dlg_S2_Ahead_10_Speaker", "上班族" },
        { "Dlg_S2_Ahead_10_Text", "那我们一起走吧。" },
        { "Dlg_S2_Ahead_11_Speaker", "死神" },
        { "Dlg_S2_Ahead_11_Text", "……你真麻烦。" },
        { "Dlg_S2_Ahead_12_Speaker", "上班族" },
        { "Dlg_S2_Ahead_12_Text", "我知道。" },
        { "ReturnConfirmMessage", "返回主菜单将丢失未保存的进度，确定吗？" },
        { "Ending_ThankYou", "感谢游玩。" },
        { "Ending_JourneyEnd", "你的旅程已经走到了尽头。" },
        { "Ending_NewBeginning", "但每一个结束都是一个新的开始。" },
        { "Ending_SeeYouAgain", "我们期待再次与你相见。" },
        { "Ending_SkipPrompt", "按任意键跳过" },
        { "Prompt_Interact", "按 Enter/X 交互" },

        // EndingCanvas Prefab
        { "EndingCanvas_SoAtLeast", "所以，至少" },
        { "EndingCanvas_SkipPrompt", "按任意键跳过" },
        { "EndingCanvas_ReaperName", "我还是不喜欢\"死神\"这个名字。" },
        { "EndingCanvas_WalkWithThem", "陪他们走完旅程的最后一段路。" },
        { "EndingCanvas_ICanStay", "我可以留下来。" },
        { "EndingCanvas_HateHelpless", "我还是讨厌什么都做不了的感觉。" },
        { "EndingCanvas_TheEnd", "终" },
        { "EndingCanvas_CantChange", "有些事我真的无法改变。" },
        { "EndingCanvas_Feared", "我还是不喜欢人们害怕我的样子。" },
    };
}
