using System;
using System.Collections;
using UnityEngine;

namespace EnglishTek.Core
{
    public enum UIGroupAnimation
    {
        None,
        Fade,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        ScalePop,
        FadeSlideUp,       // fade in + rise from below
        FadeSlideDown,     // fade in + drop from above
        FadeScalePop,      // fade in + scale pop from center
        SlideUp,           // slide upward only, no fade (exit only)
    }

    /// <summary>
    /// Attach to any UI container to give it animated Show / Hide support.
    ///   animationIn  → controls how Show() plays (None = instant snap).
    ///   animationOut → controls how Hide() plays (None = no hide, stays visible).
    /// </summary>
    [DisallowMultipleComponent]
    public class UIGroup : MonoBehaviour
    {
        [Tooltip("Animation played when this group is shown.")]
        [SerializeField] private UIGroupAnimation animationIn = UIGroupAnimation.Fade;

        [Tooltip("Animation played when this group is hidden. None = stay visible (no hide).")]
        [SerializeField] private UIGroupAnimation animationOut = UIGroupAnimation.Fade;

        [SerializeField] private float duration = 0.25f;

        [Tooltip("How far (pixels) a sliding element starts from its final position.")]
        [SerializeField] private float slideDistance = 60f;

        [Tooltip("Deactivate this group when the scene starts.")]
        [SerializeField] private bool startHidden = false;

        // ------------------------------------------------------------------ state

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector2 shownPosition;
        private Vector2 originalPosition;
        private Coroutine activeCoroutine;
        private UIGroupAnimation activeHideAnimation;

        public bool IsVisible { get; private set; }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            shownPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            originalPosition = shownPosition;

            if (startHidden)
            {
                SnapHidden();
                gameObject.SetActive(false);
                IsVisible = false;
            }
            else
            {
                SnapShown();
                IsVisible = true;
            }
        }

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Animate this group into view.
        /// None → instant snap. Any other type → plays the assigned animation.
        /// </summary>
        public void Show(Action onComplete = null)
        {
            StopActive();
            gameObject.SetActive(true);
            IsVisible = true;

            if (animationIn == UIGroupAnimation.None || duration <= 0f)
            {
                SnapShown();
                onComplete?.Invoke();
                return;
            }

            activeCoroutine = StartCoroutine(RunShow(onComplete));
        }

        /// <summary>
        /// Animate this group out of view, then deactivate it.
        /// None → does nothing; the group stays visible.
        /// Any other type → plays the assigned animation then deactivates.
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (animationOut == UIGroupAnimation.None)
            {
                // None means "no hide" — stay visible, still fire callback
                onComplete?.Invoke();
                return;
            }

            StopActive();
            IsVisible = false;
            activeHideAnimation = animationOut;
            activeCoroutine = StartCoroutine(RunHide(onComplete));
        }

        /// <summary>
        /// Hide with a one-time animation override instead of the configured animationOut.
        /// Useful when back navigation needs a different exit than the normal hide.
        /// </summary>
        public void HideWith(UIGroupAnimation overrideAnimation, Action onComplete = null)
        {
            if (overrideAnimation == UIGroupAnimation.None)
            {
                onComplete?.Invoke();
                return;
            }

            StopActive();
            IsVisible = false;
            activeHideAnimation = overrideAnimation;
            activeCoroutine = StartCoroutine(RunHide(onComplete));
        }

        /// <summary>Snap visible with no animation.</summary>
        public void ShowImmediate()
        {
            StopActive();
            gameObject.SetActive(true);
            IsVisible = true;
            SnapShown();
        }

        /// <summary>Snap hidden with no animation.</summary>
        public void HideImmediate()
        {
            StopActive();
            IsVisible = false;
            SnapHidden();
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ coroutines

        private IEnumerator RunShow(Action onComplete)
        {
            // Reset position to original so SlideUp groups re-enter from their home spot.
            shownPosition = originalPosition;
            if (rectTransform != null) rectTransform.anchoredPosition = shownPosition;
            SetStartStateForShow();

            Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            Vector3 startScale = transform.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
                TickShow(t, startPos);
                yield return null;
            }

            SnapShown();
            activeCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator RunHide(Action onComplete)
        {
            Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            Vector3 startScale = transform.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseInQuad(Mathf.Clamp01(elapsed / duration));
                TickHide(t, startPos, startScale, activeHideAnimation);
                yield return null;
            }

            // SlideUp stays visible — just lock in the new position (don't touch shownPosition).
            if (activeHideAnimation == UIGroupAnimation.SlideUp)
            {
                IsVisible = true;
            }
            else
            {
                SnapHidden();
                gameObject.SetActive(false);
            }

            activeCoroutine = null;
            onComplete?.Invoke();
        }

        // ------------------------------------------------------------------ per-frame helpers

        private void SetStartStateForShow()
        {
            switch (animationIn)
            {
                case UIGroupAnimation.Fade:
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.SlideFromLeft:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(-slideDistance, 0f);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.SlideFromRight:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(slideDistance, 0f);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.SlideFromTop:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(0f, slideDistance);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.SlideFromBottom:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(0f, -slideDistance);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.ScalePop:
                    transform.localScale = Vector3.zero;
                    canvasGroup.alpha = 1f;
                    break;
                case UIGroupAnimation.FadeSlideUp:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(0f, -slideDistance);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.FadeSlideDown:
                    if (rectTransform != null) rectTransform.anchoredPosition = shownPosition + new Vector2(0f, slideDistance);
                    canvasGroup.alpha = 0f;
                    break;
                case UIGroupAnimation.FadeScalePop:
                    transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                    canvasGroup.alpha = 0f;
                    break;
            }
        }

        private void TickShow(float t, Vector2 startPos)
        {
            switch (animationIn)
            {
                case UIGroupAnimation.Fade:
                    canvasGroup.alpha = t;
                    break;
                case UIGroupAnimation.SlideFromLeft:
                case UIGroupAnimation.SlideFromRight:
                case UIGroupAnimation.SlideFromTop:
                case UIGroupAnimation.SlideFromBottom:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, shownPosition, t);
                    canvasGroup.alpha = t;
                    break;
                case UIGroupAnimation.ScalePop:
                    transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(t));
                    break;
                case UIGroupAnimation.FadeSlideUp:
                case UIGroupAnimation.FadeSlideDown:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, shownPosition, t);
                    canvasGroup.alpha = t;
                    break;
                case UIGroupAnimation.FadeScalePop:
                    transform.localScale = Vector3.LerpUnclamped(new Vector3(0.7f, 0.7f, 1f), Vector3.one, EaseOutBack(t));
                    canvasGroup.alpha = t;
                    break;
            }
        }

        private void TickHide(float t, Vector2 startPos, Vector3 startScale, UIGroupAnimation anim)
        {
            switch (anim)
            {
                case UIGroupAnimation.Fade:
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.SlideFromLeft:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(-slideDistance, 0f), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.SlideFromRight:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(slideDistance, 0f), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.SlideFromTop:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, slideDistance), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.SlideFromBottom:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, -slideDistance), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.FadeSlideUp:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, -slideDistance), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.FadeSlideDown:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, slideDistance), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.FadeScalePop:
                    transform.localScale = Vector3.Lerp(startScale, new Vector3(0.7f, 0.7f, 1f), t);
                    canvasGroup.alpha = 1f - t;
                    break;
                case UIGroupAnimation.ScalePop:
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                    break;
                case UIGroupAnimation.SlideUp:
                    if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, slideDistance), t);
                    break;
            }
        }

        // ------------------------------------------------------------------ snap helpers

        private void SnapShown()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (rectTransform != null) rectTransform.anchoredPosition = shownPosition;
            transform.localScale = Vector3.one;
        }

        private void SnapHidden()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (rectTransform != null) rectTransform.anchoredPosition = shownPosition;
            transform.localScale = Vector3.one;
        }

        private void StopActive()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        // ------------------------------------------------------------------ easing

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInQuad(float t) => t * t;

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}