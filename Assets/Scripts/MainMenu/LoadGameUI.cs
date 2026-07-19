using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// 存档（Load Game）界面 UI 构建器。
    /// 显示最多 3 个存档槽，已存在的存档高亮可用，空槽置灰。
    /// 顶部有标题，底部有返回按钮。
    /// </summary>
    public class LoadGameUI : MonoBehaviour
    {
        [Header("外观设置")]
        public Color backgroundColor = Color.white;
        public Color buttonColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        public Color titleColor = Color.black;
        public Font overrideFont;

        [Header("尺寸设置")]
        [Range(24, 100)]
        public int titleFontSize = 56;

        [Range(16, 64)]
        public int slotFontSize = 28;

        [Range(16, 64)]
        public int backButtonFontSize = 24;

        [Range(200, 800)]
        public float slotWidth = 500f;

        [Range(30, 120)]
        public float slotHeight = 80f;

        [Range(0, 60)]
        public float spacing = 15f;

        [Header("存档设置")]
        [Range(1, 10)]
        public int saveSlotCount = 3;

        public string saveFileExtension = ".json";

        private Font EffectiveFont => overrideFont ?? MenuUIHelper.GetDefaultFont();

        void Awake()
        {
            BuildUI();
        }

        void BuildUI()
        {
            Canvas canvas = MenuUIHelper.CreateCanvas();
            MenuUIHelper.EnsureEventSystem();
            MenuUIHelper.CreateFullScreenBackground(canvas.transform, backgroundColor);
            RectTransform content = MenuUIHelper.CreateCenteredContent(canvas.transform, slotWidth, spacing);

            // 标题
            MenuUIHelper.CreateText(content, "Select Save", titleFontSize, titleColor,
                EffectiveFont, FontStyle.Bold, 100f);

            // 存档槽
            for (int i = 1; i <= saveSlotCount; i++)
            {
                int capturedIndex = i; // 闭包捕获
                bool hasSave = File.Exists(GetSavePath(i));
                string label = hasSave ? $"Save Slot {i}" : $"Save Slot {i} (Empty)";

                MenuUIHelper.CreateButton(content, label, slotFontSize, slotHeight,
                    buttonColor, () => OnSlotClicked(capturedIndex), EffectiveFont, hasSave);
            }

            // 间距
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(slotWidth, 30f);

            // 返回按钮
            MenuUIHelper.CreateButton(content, "< Back", backButtonFontSize, 50f,
                buttonColor, () => SceneManager.LoadScene("MainMenu"), EffectiveFont, true);
        }

        string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_{slot}{saveFileExtension}");
        }

        void OnSlotClicked(int slot)
        {
            Debug.Log($"选中存档槽 {slot}，准备加载游戏...");
            // TODO：在此处读取实际存档数据并切换到游戏场景
            // 示例：SaveSystem.Load(slot); SceneManager.LoadScene("Gameplay");
        }
    }
}
