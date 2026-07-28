using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景结尾序列控制器。
/// 在指定对话组结束后，等待一段时间，执行淡出到黑屏，然后切换场景。
/// </summary>
public class SceneEndSequence : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("要监听的 DialogueController")]
    [SerializeField] private DialogueController dialogueController;

    [Tooltip("触发结尾序列的对话组 ID（留空则监听任何对话结束）")]
    [SerializeField] private string targetDialogueGroupId = "Dlg_S2_Ahead";

    [Header("Timing")]
    [Tooltip("对话结束后等待多少秒开始淡出")]
    [SerializeField] private float waitAfterDialogue = 3f;

    [Tooltip("淡出到黑屏的持续时间（秒）")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("Scene Transition")]
    [Tooltip("淡出完成后加载的场景名")]
    [SerializeField] private string targetSceneName = "Ending";

    private bool sequenceStarted;

    private void Start()
    {
        if (dialogueController == null)
        {
            dialogueController = FindFirstObjectByType<DialogueController>();
        }

        if (dialogueController != null)
        {
            dialogueController.OnDialogueEnded += OnDialogueEnded;
        }
        else
        {
            Debug.LogWarning("[SceneEndSequence] No DialogueController found or assigned.");
        }
    }

    private void OnDestroy()
    {
        if (dialogueController != null)
        {
            dialogueController.OnDialogueEnded -= OnDialogueEnded;
        }
    }

    private void OnDialogueEnded()
    {
        if (sequenceStarted) return;

        // 如果指定了目标 groupId，检查是否匹配
        if (!string.IsNullOrEmpty(targetDialogueGroupId))
        {
            if (dialogueController.CurrentDialogueGroupId != targetDialogueGroupId)
            {
                return;
            }
        }

        sequenceStarted = true;
        StartCoroutine(PlayEndSequence());
    }

    private IEnumerator PlayEndSequence()
    {
        // 等待
        yield return new WaitForSeconds(waitAfterDialogue);

        // 创建淡出 Canvas（在最顶层）
        GameObject fadeGO = new GameObject("FadeToBlack");
        Canvas fadeCanvas = fadeGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;

        Image fadeImage = fadeGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 渐变到黑色
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }
        fadeImage.color = Color.black;

        // 稍微停顿一下确保黑屏稳定
        yield return new WaitForSeconds(0.5f);

        // 切换场景
        SceneManager.LoadScene(targetSceneName);
    }
}
