using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MainMenu
{
    public class ConfirmDialogUI : MonoBehaviour
    {
        private PlayerInputActions inputActions;
        private UnityAction storedOnCancel;

        public static void Show(string message, UnityAction onConfirm, UnityAction onCancel = null)
        {
            GameObject dialogObject = new GameObject("ConfirmDialog");
            ConfirmDialogUI dialog = dialogObject.AddComponent<ConfirmDialogUI>();
            dialog.storedOnCancel = onCancel;
            dialog.Build(message, onConfirm, onCancel);
        }

        private void OnEnable()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Menu.performed += OnMenuPerformed;
            inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                return;
            }

            inputActions.Player.Menu.performed -= OnMenuPerformed;
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            storedOnCancel?.Invoke();
            Destroy(gameObject);
        }

        private void Build(string message, UnityAction onConfirm, UnityAction onCancel)
        {
            if (TryBuildPrefabUI(message, onConfirm, onCancel))
            {
                return;
            }

            Canvas canvas = MenuUIHelper.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            canvas.sortingOrder = 999;

            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, new Color(0f, 0f, 0f, 0.5f));

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

            Text messageText = MenuUIHelper.CreateText(panel.transform, message, 24, Color.white,
                MenuUIHelper.GetDefaultFont(), FontStyle.Normal, 140f);
            RectTransform messageRect = messageText.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 0.65f);
            messageRect.anchorMax = new Vector2(0.5f, 0.65f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = Vector2.zero;
            messageRect.sizeDelta = new Vector2(580f, 140f);

            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.22f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.22f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(520f, 60f);

            Font font = MenuUIHelper.GetDefaultFont();
            Button confirmButton = MenuUIHelper.CreateButton(buttonContainer.transform, "Confirm", 22, 55f,
                new Color(0.2f, 0.55f, 0.3f, 1f),
                () => { onConfirm?.Invoke(); Destroy(gameObject); },
                font, true);
            RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0f, 0.5f);
            confirmRect.anchorMax = new Vector2(0f, 0.5f);
            confirmRect.pivot = new Vector2(0f, 0.5f);
            confirmRect.anchoredPosition = Vector2.zero;
            confirmRect.sizeDelta = new Vector2(220f, 55f);

            Button cancelButton = MenuUIHelper.CreateButton(buttonContainer.transform, "Cancel", 22, 55f,
                new Color(0.55f, 0.2f, 0.2f, 1f),
                () => { onCancel?.Invoke(); Destroy(gameObject); },
                font, true);
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(1f, 0.5f);
            cancelRect.anchorMax = new Vector2(1f, 0.5f);
            cancelRect.pivot = new Vector2(1f, 0.5f);
            cancelRect.anchoredPosition = Vector2.zero;
            cancelRect.sizeDelta = new Vector2(220f, 55f);
        }

        private bool TryBuildPrefabUI(string message, UnityAction onConfirm, UnityAction onCancel)
        {
            // 暂时禁用 prefab 路径：MessageText 的 TMP 组件在运行时存在渲染问题，
            // fallback 生成的 UI 使用 legacy Text，显示正常。
            return false;
        }
    }
}
