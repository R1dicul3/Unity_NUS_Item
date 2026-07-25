using System.Collections;
using UnityEngine;

namespace MainMenu
{
    /// <summary>
    /// Credits 分页轮播器。将 Credits 内容分为若干部分，依次淡入淡出循环展示。
    /// </summary>
    public class CreditsCarousel : MonoBehaviour
    {
        [Header("分页内容")]
        [Tooltip("将 Credits 的每个部分拖到这里，按展示顺序排列。")]
        public GameObject[] parts;

        [Header("时间设置")]
        [Tooltip("每部分停留显示的时长（秒）。")]
        [SerializeField] private float displayDuration = 5f;

        [Tooltip("淡入/淡出的过渡时长（秒）。")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("首次开始前的等待时间（秒）。")]
        [SerializeField] private float startDelay = 1f;

        private int _currentIndex = -1;
        private float _timer;
        private CanvasGroup[] _canvasGroups;
        private bool _isTransitioning;
        private bool _started;

        private void Start()
        {
            if (parts == null || parts.Length == 0)
            {
                Debug.LogWarning("[CreditsCarousel] 没有设置分页内容（parts 为空）。");
                enabled = false;
                return;
            }

            _canvasGroups = new CanvasGroup[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null)
                {
                    continue;
                }

                CanvasGroup cg = parts[i].GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = parts[i].AddComponent<CanvasGroup>();
                }

                _canvasGroups[i] = cg;
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }

            _timer = 0f;
            _started = false;
        }

        private void Update()
        {
            if (_isTransitioning)
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
                    ShowPart(0);
                }
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= displayDuration)
            {
                _timer = 0f;
                int nextIndex = (_currentIndex + 1) % parts.Length;
                StartCoroutine(TransitionTo(nextIndex));
            }
        }

        private void ShowPart(int index)
        {
            if (index < 0 || index >= _canvasGroups.Length)
            {
                return;
            }

            _currentIndex = index;

            for (int i = 0; i < _canvasGroups.Length; i++)
            {
                if (_canvasGroups[i] == null)
                {
                    continue;
                }

                bool isActive = i == index;
                _canvasGroups[i].alpha = isActive ? 1f : 0f;
                _canvasGroups[i].blocksRaycasts = isActive;
            }
        }

        private IEnumerator TransitionTo(int nextIndex)
        {
            _isTransitioning = true;

            CanvasGroup from = _currentIndex >= 0 && _currentIndex < _canvasGroups.Length
                ? _canvasGroups[_currentIndex]
                : null;
            CanvasGroup to = nextIndex >= 0 && nextIndex < _canvasGroups.Length
                ? _canvasGroups[nextIndex]
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
        }
    }
}
