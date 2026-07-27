using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public enum DisplayMode
    {
        FadeAll,
        TypewriterLines
    }

    [System.Serializable]
    public struct EndingPage
    {
        [Tooltip("该页根物体，子物体为逐行显示的每一行")]
        public GameObject content;
        [Tooltip("显示模式：FadeAll=整页淡入淡出，TypewriterLines=逐行淡入")]
        public DisplayMode displayMode;
        [Tooltip("FadeAll：页面停留时长（秒）；TypewriterLines：每行显示后的间隔（秒）")]
        public float displayDuration;
    }

    /// <summary>
    /// 结束界面分页轮播器。
    /// 支持 FadeAll（整页淡入淡出）和 TypewriterLines（逐行淡入）两种显示模式。
    /// 所有页展示完毕后触发 OnAllPagesShown 事件。
    /// </summary>
    public class EndingCarousel : MonoBehaviour
    {

        [Header("分页内容")]
        [Tooltip("将结束界面的每页拖到这里，按展示顺序排列")]
        public EndingPage[] pages;

        [Header("时间设置")]
        [Tooltip("首次开始前的等待时间（秒）")]
        [SerializeField] private float startDelay = 1f;

        [Tooltip("页面淡入/淡出的过渡时长（秒）")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("逐行模式下每行的淡入时长（秒）")]
        [SerializeField] private float lineFadeDuration = 0.3f;

        [Tooltip("逐行模式全部行显示完后的停留时间（秒）")]
        [SerializeField] private float pagePostDelay = 1.5f;

        [Header("跳过")]
        [Tooltip("是否启用跳过提示功能")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("跳过提示文本物体（默认隐藏，首次按键后显示）")]
        public GameObject skipPrompt;

        [Tooltip("跳过提示显示后多久自动隐藏（秒）")]
        [SerializeField] private float skipPromptVisibleDuration = 2f;

        public event System.Action OnAllPagesShown;

        private int _currentIndex = -1;
        private float _timer;
        private bool _started;
        private bool _isTransitioning;
        private bool _isShowingLines;
        private CanvasGroup[] _pageCanvasGroups;
        private Dictionary<GameObject, CanvasGroup[]> _lineGroupsMap = new Dictionary<GameObject, CanvasGroup[]>();
        private Coroutine _currentLineCoroutine;
        private bool _skipPromptActive;
        private float _skipPromptTimer;

        private void Start()
        {
            if (pages == null || pages.Length == 0)
            {
                Debug.LogWarning("[EndingCarousel] 没有设置分页内容（pages 为空）。");
                enabled = false;
                return;
            }

            _pageCanvasGroups = new CanvasGroup[pages.Length];
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i].content == null)
                {
                    continue;
                }

                CanvasGroup cg = pages[i].content.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = pages[i].content.AddComponent<CanvasGroup>();
                }

                _pageCanvasGroups[i] = cg;
                cg.alpha = 0f;
                cg.blocksRaycasts = false;

                // 为 TypewriterLines 模式的页面预准备子行 CanvasGroup
                if (pages[i].displayMode == DisplayMode.TypewriterLines)
                {
                    PrepareLineCanvasGroups(pages[i].content);
                }
            }

            _timer = 0f;
            _started = false;
            _skipPromptActive = false;
            _skipPromptTimer = 0f;

            if (skipPrompt != null)
            {
                skipPrompt.SetActive(false);
            }
        }

        private void PrepareLineCanvasGroups(GameObject pageRoot)
        {
            if (pageRoot == null) return;

            List<CanvasGroup> lineGroups = new List<CanvasGroup>();
            foreach (Transform child in pageRoot.transform)
            {
                if (child == null) continue;
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = child.gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = 0f;
                lineGroups.Add(cg);
            }

            _lineGroupsMap[pageRoot] = lineGroups.ToArray();
        }

        private void Update()
        {
            // 跳过提示的输入检测独立于页面状态
            HandleSkipPrompt();

            if (_isTransitioning || _isShowingLines)
            {
                return;
            }

            if (!_started)
            {
                _timer += Time.deltaTime;
                if (_timer >= startDelay)
                {
                    _started = true;
                    _timer = 0f;
                    StartCoroutine(TransitionTo(0));
                }
                return;
            }

            // FadeAll 模式计时
            if (_currentIndex >= 0 && _currentIndex < pages.Length)
            {
                if (pages[_currentIndex].displayMode == DisplayMode.FadeAll)
                {
                    _timer += Time.deltaTime;
                    if (_timer >= pages[_currentIndex].displayDuration)
                    {
                        _timer = 0f;
                        GoToNextPage();
                    }
                }
            }
        }

        private void HandleSkipPrompt()
        {
            if (!allowSkip || skipPrompt == null)
            {
                return;
            }

            // 页面切换期间不响应跳过提示
            if (_isTransitioning)
            {
                return;
            }

            // 跳过提示计时（自动隐藏）
            if (_skipPromptActive)
            {
                _skipPromptTimer += Time.deltaTime;
                if (_skipPromptTimer >= skipPromptVisibleDuration)
                {
                    skipPrompt.SetActive(false);
                    _skipPromptActive = false;
                }
            }

            // 检测按键
            if (Input.anyKeyDown)
            {
                if (!_skipPromptActive)
                {
                    // 第一次按键：显示提示
                    skipPrompt.SetActive(true);
                    _skipPromptActive = true;
                    _skipPromptTimer = 0f;
                }
                else
                {
                    // 提示已显示：执行跳过
                    SkipCurrentPage();
                }
            }
        }

        private IEnumerator TransitionTo(int nextIndex)
        {
            _isTransitioning = true;

            CanvasGroup from = _currentIndex >= 0 && _currentIndex < _pageCanvasGroups.Length
                ? _pageCanvasGroups[_currentIndex]
                : null;
            CanvasGroup to = nextIndex >= 0 && nextIndex < _pageCanvasGroups.Length
                ? _pageCanvasGroups[nextIndex]
                : null;

            if (to != null)
            {
                to.blocksRaycasts = true;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                if (from != null)
                {
                    from.alpha = Mathf.Lerp(1f, 0f, t);
                }
                if (to != null)
                {
                    to.alpha = Mathf.Lerp(0f, 1f, t);
                }

                yield return null;
            }

            if (from != null)
            {
                from.alpha = 0f;
                from.blocksRaycasts = false;
            }
            if (to != null)
            {
                to.alpha = 1f;
            }

            _currentIndex = nextIndex;
            _isTransitioning = false;
            _timer = 0f;

            // 如果新页是 TypewriterLines，启动逐行显示
            if (pages[nextIndex].displayMode == DisplayMode.TypewriterLines)
            {
                _currentLineCoroutine = StartCoroutine(ShowLinesCoroutine(pages[nextIndex].content, pages[nextIndex].displayDuration));
            }
        }

        private IEnumerator ShowLinesCoroutine(GameObject pageRoot, float lineInterval)
        {
            _isShowingLines = true;

            if (_lineGroupsMap.TryGetValue(pageRoot, out CanvasGroup[] lineGroups))
            {
                // 确保所有行初始隐藏
                foreach (var cg in lineGroups)
                {
                    if (cg != null) cg.alpha = 0f;
                }

                for (int i = 0; i < lineGroups.Length; i++)
                {
                    if (lineGroups[i] == null) continue;

                    // 淡入当前行
                    float elapsed = 0f;
                    while (elapsed < lineFadeDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / lineFadeDuration;
                        lineGroups[i].alpha = Mathf.Lerp(0f, 1f, t);
                        yield return null;
                    }
                    lineGroups[i].alpha = 1f;

                    // 等待间隔（最后一行后不等待）
                    if (i < lineGroups.Length - 1)
                    {
                        float waitTimer = 0f;
                        while (waitTimer < lineInterval)
                        {
                            waitTimer += Time.deltaTime;
                            yield return null;
                        }
                    }
                }
            }

            // 所有行显示完后，等待 pagePostDelay
            float postDelay = 0f;
            while (postDelay < pagePostDelay)
            {
                postDelay += Time.deltaTime;
                yield return null;
            }

            _isShowingLines = false;
            _currentLineCoroutine = null;
            GoToNextPage();
        }

        private void GoToNextPage()
        {
            int nextIndex = _currentIndex + 1;
            if (nextIndex >= pages.Length)
            {
                Finish();
                return;
            }
            StartCoroutine(TransitionTo(nextIndex));
        }

        public void SkipCurrentPage()
        {
            if (_currentIndex < 0 || _currentIndex >= pages.Length)
            {
                return;
            }

            // 隐藏跳过提示
            if (skipPrompt != null)
            {
                skipPrompt.SetActive(false);
                _skipPromptActive = false;
            }

            if (_currentLineCoroutine != null)
            {
                StopCoroutine(_currentLineCoroutine);
                _currentLineCoroutine = null;

                // 立即显示完当前页所有行
                if (_lineGroupsMap.TryGetValue(pages[_currentIndex].content, out CanvasGroup[] lineGroups))
                {
                    foreach (var cg in lineGroups)
                    {
                        if (cg != null) cg.alpha = 1f;
                    }
                }
                _isShowingLines = false;
            }

            GoToNextPage();
        }

        private void Finish()
        {
            OnAllPagesShown?.Invoke();
        }
    }
}
