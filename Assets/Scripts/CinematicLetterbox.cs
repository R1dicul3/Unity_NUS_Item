using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CinematicLetterbox : MonoBehaviour {
    [SerializeField] private Image topBar;
    [SerializeField] private Image bottomBar;
    [SerializeField] private float barHeight = 130f;
    [SerializeField] private float animationSeconds = 0.24f;
    [SerializeField] private Color barColor = Color.black;

    private Coroutine animationRoutine;

    public float AnimationSeconds => Mathf.Max(0.01f, animationSeconds);

    private void Awake() {
        EnsureBars();
        ApplyLayout();
    }

    private void OnValidate() {
        EnsureBars();
        ApplyLayout();
    }

    public void Show(Action onComplete = null) {
        Play(true, onComplete);
    }

    public void Hide(Action onComplete = null) {
        Play(false, onComplete);
    }

    public void SetVisibleImmediate(bool visible) {
        StopAnimation();
        EnsureBars();
        ApplyLayout();
        SetHeight(visible ? barHeight : 0f);
    }

    public void EnsureBars() {
        if (topBar == null) {
            topBar = FindBar("CinematicTopBar");
        }

        if (bottomBar == null) {
            bottomBar = FindBar("CinematicBottomBar");
        }

        if (topBar == null) {
            topBar = CreateBar("CinematicTopBar", true);
        }

        if (bottomBar == null) {
            bottomBar = CreateBar("CinematicBottomBar", false);
        }
    }

    public void ApplyLayout() {
        if (topBar == null || bottomBar == null) {
            return;
        }

        ApplyRootLayout();
        ApplyBarLayout(topBar.rectTransform, true);
        ApplyBarLayout(bottomBar.rectTransform, false);

        topBar.color = barColor;
        bottomBar.color = barColor;
        topBar.raycastTarget = false;
        bottomBar.raycastTarget = false;
    }

    private void Play(bool visible, Action onComplete) {
        EnsureBars();
        ApplyLayout();

        if (topBar == null || bottomBar == null) {
            onComplete?.Invoke();
            return;
        }

        if (!Application.isPlaying) {
            SetVisibleImmediate(visible);
            onComplete?.Invoke();
            return;
        }

        StopAnimation();
        animationRoutine = StartCoroutine(Animate(visible, onComplete));
    }

    private IEnumerator Animate(bool visible, Action onComplete) {
        float from = topBar.rectTransform.sizeDelta.y;
        float to = visible ? barHeight : 0f;
        float elapsed = 0f;

        while (elapsed < AnimationSeconds) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / AnimationSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            SetHeight(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        SetHeight(to);
        animationRoutine = null;
        onComplete?.Invoke();
    }

    private void StopAnimation() {
        if (animationRoutine == null) {
            return;
        }

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private Image FindBar(string barName) {
        Transform bar = transform.Find(barName);
        return bar != null ? bar.GetComponent<Image>() : null;
    }

    private Image CreateBar(string barName, bool top) {
        GameObject barObject = new GameObject(barName);
        barObject.transform.SetParent(transform, false);

        RectTransform rect = barObject.AddComponent<RectTransform>();
        ApplyBarLayout(rect, top);
        rect.sizeDelta = Vector2.zero;

        Image image = barObject.AddComponent<Image>();
        image.color = barColor;
        image.raycastTarget = false;
        return image;
    }

    private void ApplyRootLayout() {
        RectTransform rect = transform as RectTransform;
        if (rect == null) {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ApplyBarLayout(RectTransform rect, bool top) {
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
    }

    private void SetHeight(float height) {
        if (topBar == null || bottomBar == null) {
            return;
        }

        SetBarHeight(topBar.rectTransform, height);
        SetBarHeight(bottomBar.rectTransform, height);
    }

    private static void SetBarHeight(RectTransform rect, float height) {
        Vector2 size = rect.sizeDelta;
        size.y = height;
        rect.sizeDelta = size;
    }
}
