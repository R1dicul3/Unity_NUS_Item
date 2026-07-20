using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 通用确认对话框。运行时动态创建，点击按钮后自动销毁。
    /// </summary>
    public class ConfirmDialogUI : MonoBehaviour
    {
        private PlayerInputActions inputActions;
        private UnityAction storedOnCancel;

        /// <summary>
        /// 显示确认对话框。
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="onConfirm">点击确定时的回调</param>
        /// <param name="onCancel">点击取消时的回调（可为 null）</param>
        public static void Show(string message, UnityAction onConfirm, UnityAction onCancel = null)
        {
            GameObject go = new GameObject("ConfirmDialog");
            var dialog = go.AddComponent<ConfirmDialogUI>();
            dialog.storedOnCancel = onCancel;
            dialog.Build(message, onConfirm, onCancel);
        }

        void OnEnable()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Menu.performed += OnMenuPerformed;
            inputActions.Enable();
        }

        void OnDisable()
        {
            if (inputActions != null)
            {
                inputActions.Player.Menu.performed -= OnMenuPerformed;
                inputActions.Disable();
                inputActions.Dispose();
                inputActions = null;
            }
        }

        void OnMenuPerformed(InputAction.CallbackContext context)
        {
            storedOnCancel?.Invoke();
            Destroy(gameObject);
        }

        private void Build(string message, UnityAction onConfirm, UnityAction onCancel)
        {
            // 创建独立 Canvas，确保在最上层
            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 999;

            MenuUIHelper.EnsureEventSystem();

            // 半透明黑色背景（阻挡点击）
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, new Color(0f, 0f, 0f, 0.5f));

            // 对话框面板
            GameObject panel = new GameObject("DialogPanel");
            panel.transform.SetParent(canvas.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(640f, 320f);

            // 消息文本
            Text msgText = MenuUIHelper.CreateText(panel.transform, message, 24, Color.white,
                MenuUIHelper.GetDefaultFont(), FontStyle.Normal, 140f);
            RectTransform msgRect = msgText.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.65f);
            msgRect.anchorMax = new Vector2(0.5f, 0.65f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(580f, 140f);

            // 按钮容器（手动定位，不使用 HorizontalLayoutGroup）
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(panel.transform, false);
            RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.22f);
            btnRect.anchorMax = new Vector2(0.5f, 0.22f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(520f, 60f);

            Font font = MenuUIHelper.GetDefaultFont();

            // 确定按钮（左侧）
            Button confirmBtn = MenuUIHelper.CreateButton(btnContainer.transform, "确定", 22, 55f,
                new Color(0.2f, 0.55f, 0.3f, 1f),
                () => { onConfirm?.Invoke(); Destroy(gameObject); },
                font, true);
            RectTransform confirmRect = confirmBtn.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0f, 0.5f);
            confirmRect.anchorMax = new Vector2(0f, 0.5f);
            confirmRect.pivot = new Vector2(0f, 0.5f);
            confirmRect.anchoredPosition = Vector2.zero;
            confirmRect.sizeDelta = new Vector2(220f, 55f);

            // 取消按钮（右侧）
            Button cancelBtn = MenuUIHelper.CreateButton(btnContainer.transform, "取消", 22, 55f,
                new Color(0.55f, 0.2f, 0.2f, 1f),
                () => { onCancel?.Invoke(); Destroy(gameObject); },
                font, true);
            RectTransform cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(1f, 0.5f);
            cancelRect.anchorMax = new Vector2(1f, 0.5f);
            cancelRect.pivot = new Vector2(1f, 0.5f);
            cancelRect.anchoredPosition = Vector2.zero;
            cancelRect.sizeDelta = new Vector2(220f, 55f);
        }
    }
}
