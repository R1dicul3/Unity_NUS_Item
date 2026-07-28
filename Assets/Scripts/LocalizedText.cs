using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 本地化文本组件。
/// 挂载到任何带有 Text 或 TextMeshProUGUI 的 UI 物体上，
/// 在 Start 时自动根据 localizationKey 刷新文本，并监听语言切换事件。
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    private Text legacyText;
    private TextMeshProUGUI tmpText;

    private void Start()
    {
        legacyText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
        Refresh();

        LocalizationManager.OnLanguageChanged += Refresh;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        if (string.IsNullOrEmpty(localizationKey))
            return;

        string value = LocalizationManager.Get(localizationKey);

        if (tmpText != null)
        {
            tmpText.text = value;
            tmpText.SetAllDirty();
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
}
