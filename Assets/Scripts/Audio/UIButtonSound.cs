using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 按钮音效组件。
/// 自动处理鼠标悬停、手柄/键盘导航选中和点击的音效播放。
/// 挂载到任意 Button 上即可生效，通常由 MenuUIHelper 自动添加。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
        }
    }

    /// <summary>
    /// 鼠标指针进入按钮区域时播放悬停音效。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 只有当 EventSystem 当前选中的不是本按钮时才播放，
        // 避免手柄选中后再移动鼠标导致重复音效。
        if (EventSystem.current?.currentSelectedGameObject != gameObject)
        {
            AudioManager.Instance?.PlayOneShot(SoundType.UIHover);
        }
    }

    /// <summary>
    /// 手柄/键盘导航选中此按钮时播放选中音效。
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        AudioManager.Instance?.PlayOneShot(SoundType.UISelect);
    }

    private void OnClick()
    {
        AudioManager.Instance?.PlayOneShot(SoundType.UIClick);
    }
}
