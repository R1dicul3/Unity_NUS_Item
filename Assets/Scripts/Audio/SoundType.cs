/// <summary>
/// 游戏中所有可播放的音效与音乐类型的枚举。
/// 在 SoundLibrary ScriptableObject 中为每种类型配置对应的 AudioClip。
/// </summary>
public enum SoundType
{
    None,

    // UI 音效
    UIClick,
    UIHover,
    UISelect,
    UIAlert,
    UISuccess,
    UIFail,

    // 玩家动作音效
    Jump,
    DoubleJump,
    Land,
    Dash,

    // 游戏玩法音效
    CollectItem,
    CharacterSwitch,
    SwitchVariant,
    DoorOpen,

    // 背景音乐
    MainMenuMusic,
    GameplayMusic,
    PauseMusic,
}
