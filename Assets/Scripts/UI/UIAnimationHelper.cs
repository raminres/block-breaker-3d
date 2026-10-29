using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Coordinates arcade UI motion, snappy scale pop-ins, staggered top-to-bottom cascades,
    /// and tactile button feedback across UI Toolkit screens.
    /// </summary>
    public static class UIAnimationHelper
    {
        public const string ClassBtnPopHidden = "ui-btn-pop-hidden";
        public const string ClassBtnPopVisible = "ui-btn-pop-visible";
        public const string ClassModalCardHidden = "modal-card-hidden";
        public const string ClassModalCardVisible = "modal-card-visible";
        public const string ClassModalBackdropHidden = "modal-backdrop-hidden";
        public const string ClassModalBackdropVisible = "modal-backdrop-visible";
        public const string ClassBtnPunched = "ui-btn-punched";
        public const string ClassLaunchFingerLeft = "launch-finger-swipe-left";
        public const string ClassLaunchFingerRight = "launch-finger-swipe-right";

        private static readonly Dictionary<VisualElement, Coroutine> activeCoroutines = new Dictionary<VisualElement, Coroutine>();

        /// <summary>
        /// True when an async scene transition faded to dark and the newly loaded scene should fade in.
        /// </summary>
        public static bool PendingSceneFadeIn { get; set; }

        /// <summary>
        /// True while an async scene transition is actively fading out or loading.
        /// </summary>
        public static bool IsSceneTransitioning { get; private set; }

        /// <summary>
        /// Plays a subsequent top-to-bottom scaling pop-in on a list of elements (e.g. buttons).
        /// Each element expands from scale 0.65 to 1.0 with a snappy spring overshoot.
        /// </summary>
        public static Coroutine CascadeIn(
            MonoBehaviour host,
            IList<VisualElement> items,
            float staggerDelay = 0.035f,
            float startDelay = 0.02f,
            Action onComplete = null)
        {
            if (items == null || items.Count == 0)
            {
                onComplete?.Invoke();
                return null;
            }

            // EditMode fallback: apply visible state synchronously
            if (!Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].RemoveFromClassList(ClassBtnPopHidden);
                        items[i].AddToClassList(ClassBtnPopVisible);
                    }
                }
                onComplete?.Invoke();
                return null;
            }

            return host.StartCoroutine(DoCascadeIn(items, staggerDelay, startDelay, onComplete));
        }

        private static IEnumerator DoCascadeIn(
            IList<VisualElement> items,
            float staggerDelay,
            float startDelay,
            Action onComplete)
        {
            // Initial frame: ensure all items are in hidden pop state
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].RemoveFromClassList(ClassBtnPopVisible);
                    items[i].AddToClassList(ClassBtnPopHidden);
                }
            }

            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);
            else
                yield return null;

            // Sequential pop from top to bottom
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null)
                {
                    item.RemoveFromClassList(ClassBtnPopHidden);
                    item.AddToClassList(ClassBtnPopVisible);
                }

                if (staggerDelay > 0f && i < items.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(staggerDelay);
                }
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Plays a short scale-down exit on a list of elements.
        /// </summary>
        public static Coroutine CascadeOut(
            MonoBehaviour host,
            IList<VisualElement> items,
            float duration = 0.10f,
            Action onComplete = null)
        {
            if (items == null || items.Count == 0)
            {
                onComplete?.Invoke();
                return null;
            }

            if (!Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].RemoveFromClassList(ClassBtnPopVisible);
                        items[i].AddToClassList(ClassBtnPopHidden);
                    }
                }
                onComplete?.Invoke();
                return null;
            }

            return host.StartCoroutine(DoCascadeOut(items, duration, onComplete));
        }

        private static IEnumerator DoCascadeOut(
            IList<VisualElement> items,
            float duration,
            Action onComplete)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].RemoveFromClassList(ClassBtnPopVisible);
                    items[i].AddToClassList(ClassBtnPopHidden);
                }
            }

            yield return new WaitForSecondsRealtime(duration);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Shows a modal with backdrop fade-in, card spring pop-in, and subsequent button cascade.
        /// </summary>
        public static Coroutine ShowModal(
            MonoBehaviour host,
            VisualElement modalBackdrop,
            VisualElement modalCard,
            IList<VisualElement> cascadeButtons = null,
            Action onComplete = null)
        {
            if (modalBackdrop == null)
            {
                onComplete?.Invoke();
                return null;
            }

            modalBackdrop.RemoveFromClassList("modal-hidden");
            modalBackdrop.pickingMode = PickingMode.Position;

            if (modalCard != null)
            {
                var title = modalCard.Q<Label>(className: "modal-title");
                if (title != null)
                {
                    AutoFitTextToWidth(title);
                }
            }

            if (!Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
            {
                modalBackdrop.RemoveFromClassList(ClassModalBackdropHidden);
                modalBackdrop.AddToClassList(ClassModalBackdropVisible);
                if (modalCard != null)
                {
                    modalCard.RemoveFromClassList(ClassModalCardHidden);
                    modalCard.AddToClassList(ClassModalCardVisible);
                }
                if (cascadeButtons != null)
                {
                    for (int i = 0; i < cascadeButtons.Count; i++)
                    {
                        if (cascadeButtons[i] != null)
                        {
                            cascadeButtons[i].RemoveFromClassList(ClassBtnPopHidden);
                            cascadeButtons[i].AddToClassList(ClassBtnPopVisible);
                        }
                    }
                }
                onComplete?.Invoke();
                return null;
            }

            // Stop any previous animation running on this backdrop
            StopActiveAnimation(host, modalBackdrop);

            var coroutine = host.StartCoroutine(DoShowModal(host, modalBackdrop, modalCard, cascadeButtons, onComplete));
            activeCoroutines[modalBackdrop] = coroutine;
            return coroutine;
        }

        private static IEnumerator DoShowModal(
            MonoBehaviour host,
            VisualElement modalBackdrop,
            VisualElement modalCard,
            IList<VisualElement> cascadeButtons,
            Action onComplete)
        {
            // Prepare initial state
            modalBackdrop.AddToClassList(ClassModalBackdropHidden);
            modalBackdrop.RemoveFromClassList(ClassModalBackdropVisible);

            if (modalCard != null)
            {
                modalCard.AddToClassList(ClassModalCardHidden);
                modalCard.RemoveFromClassList(ClassModalCardVisible);
            }

            if (cascadeButtons != null)
            {
                for (int i = 0; i < cascadeButtons.Count; i++)
                {
                    if (cascadeButtons[i] != null)
                    {
                        cascadeButtons[i].AddToClassList(ClassBtnPopHidden);
                        cascadeButtons[i].RemoveFromClassList(ClassBtnPopVisible);
                    }
                }
            }

            yield return null;

            // Trigger modal card pop-in and backdrop fade-in
            modalBackdrop.RemoveFromClassList(ClassModalBackdropHidden);
            modalBackdrop.AddToClassList(ClassModalBackdropVisible);

            if (modalCard != null)
            {
                modalCard.RemoveFromClassList(ClassModalCardHidden);
                modalCard.AddToClassList(ClassModalCardVisible);
            }

            // Stagger buttons slightly after card expands
            if (cascadeButtons != null && cascadeButtons.Count > 0 && host != null)
            {
                CascadeIn(host, cascadeButtons, 0.035f, 0.04f);
            }

            yield return new WaitForSecondsRealtime(0.18f);
            activeCoroutines.Remove(modalBackdrop);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Hides a modal with a quick, punchy scale-down (~0.10s) before applying modal-hidden.
        /// </summary>
        public static Coroutine HideModal(
            MonoBehaviour host,
            VisualElement modalBackdrop,
            VisualElement modalCard,
            Action onComplete = null)
        {
            if (modalBackdrop == null)
            {
                onComplete?.Invoke();
                return null;
            }

            // EditMode synchronous fallback
            if (!Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
            {
                modalBackdrop.AddToClassList("modal-hidden");
                onComplete?.Invoke();
                return null;
            }

            StopActiveAnimation(host, modalBackdrop);

            var coroutine = host.StartCoroutine(DoHideModal(modalBackdrop, modalCard, onComplete));
            activeCoroutines[modalBackdrop] = coroutine;
            return coroutine;
        }

        private static IEnumerator DoHideModal(
            VisualElement modalBackdrop,
            VisualElement modalCard,
            Action onComplete)
        {
            // Immediately ignore pointer events so input does not get trapped during exit
            modalBackdrop.pickingMode = PickingMode.Ignore;

            if (modalCard != null)
            {
                modalCard.RemoveFromClassList(ClassModalCardVisible);
                modalCard.AddToClassList(ClassModalCardHidden);
            }

            modalBackdrop.RemoveFromClassList(ClassModalBackdropVisible);
            modalBackdrop.AddToClassList(ClassModalBackdropHidden);

            yield return new WaitForSecondsRealtime(0.10f);

            modalBackdrop.AddToClassList("modal-hidden");
            modalBackdrop.pickingMode = PickingMode.Position;
            activeCoroutines.Remove(modalBackdrop);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Plays an immediate tactile scale bounce when a button is clicked.
        /// </summary>
        public static void PunchButton(MonoBehaviour host, VisualElement button, float duration = 0.08f)
        {
            if (button == null || !Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
                return;

            host.StartCoroutine(DoButtonPunch(button, duration));
        }

        private static IEnumerator DoButtonPunch(VisualElement button, float duration)
        {
            button.AddToClassList(ClassBtnPunched);
            yield return new WaitForSecondsRealtime(duration);
            button.RemoveFromClassList(ClassBtnPunched);
        }

        /// <summary>
        /// Temporarily triggers an outward punch scale class on an element (e.g. score pod, combo badge).
        /// </summary>
        public static Coroutine PunchElement(
            MonoBehaviour host,
            VisualElement element,
            string punchClass = "score-pop",
            float duration = 0.12f)
        {
            if (element == null || !Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
                return null;

            StopActiveAnimation(host, element);
            var coroutine = host.StartCoroutine(DoElementPunch(element, punchClass, duration));
            activeCoroutines[element] = coroutine;
            return coroutine;
        }

        private static IEnumerator DoElementPunch(VisualElement element, string punchClass, float duration)
        {
            element.AddToClassList(punchClass);
            yield return new WaitForSecondsRealtime(duration);
            element.RemoveFromClassList(punchClass);
            activeCoroutines.Remove(element);
        }

        /// <summary>
        /// Gently pulses the scale of an element in a breathing loop (e.g. TAP TO LAUNCH banner).
        /// </summary>
        public static Coroutine StartBreathe(
            MonoBehaviour host,
            VisualElement element,
            string pulseClass = "launch-banner-pulse",
            float halfPeriod = 0.55f)
        {
            if (element == null || !Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
                return null;

            StopActiveAnimation(host, element);
            var coroutine = host.StartCoroutine(DoBreathe(element, pulseClass, halfPeriod));
            activeCoroutines[element] = coroutine;
            return coroutine;
        }

        private static IEnumerator DoBreathe(VisualElement element, string pulseClass, float halfPeriod)
        {
            while (element != null && element.panel != null)
            {
                element.AddToClassList(pulseClass);
                yield return new WaitForSecondsRealtime(halfPeriod);
                element.RemoveFromClassList(pulseClass);
                yield return new WaitForSecondsRealtime(halfPeriod);
            }
            if (element != null) activeCoroutines.Remove(element);
        }

        /// <summary>
        /// Continuously animates an element smoothly swaying left and right (e.g. finger drag tutorial cue).
        /// Toggles between left-swipe and right-swipe classes using USS transition timing.
        /// </summary>
        public static Coroutine StartHorizontalSwipe(
            MonoBehaviour host,
            VisualElement element,
            string leftClass = ClassLaunchFingerLeft,
            string rightClass = ClassLaunchFingerRight,
            float halfPeriod = 0.55f)
        {
            if (element == null || !Application.isPlaying || host == null || !host.gameObject.activeInHierarchy)
                return null;

            StopActiveAnimation(host, element);
            var coroutine = host.StartCoroutine(DoHorizontalSwipe(element, leftClass, rightClass, halfPeriod));
            activeCoroutines[element] = coroutine;
            return coroutine;
        }

        private static IEnumerator DoHorizontalSwipe(
            VisualElement element,
            string leftClass,
            string rightClass,
            float halfPeriod)
        {
            bool toRight = true;
            while (element != null && element.panel != null)
            {
                if (toRight)
                {
                    element.RemoveFromClassList(leftClass);
                    element.AddToClassList(rightClass);
                }
                else
                {
                    element.RemoveFromClassList(rightClass);
                    element.AddToClassList(leftClass);
                }
                toRight = !toRight;
                yield return new WaitForSecondsRealtime(halfPeriod);
            }

            if (element != null)
            {
                element.RemoveFromClassList(leftClass);
                element.RemoveFromClassList(rightClass);
                activeCoroutines.Remove(element);
            }
        }

        public static void StopActiveAnimation(MonoBehaviour host, VisualElement element)
        {
            if (element != null && activeCoroutines.TryGetValue(element, out var running) && running != null)
            {
                if (host != null) host.StopCoroutine(running);
                activeCoroutines.Remove(element);
            }
        }

        /// <summary>
        /// Automatically adjusts a label's font size so that its text fits cleanly within
        /// its parent VisualElement width without overflowing or clipping.
        /// </summary>
        /// <param name="label">The UI Toolkit label to adjust.</param>
        /// <param name="maxFontSize">The base / default maximum font size (default: 64px).</param>
        /// <param name="minFontSize">The lowest allowed font size before clipping (default: 28px).</param>
        /// <param name="paddingBuffer">Additional horizontal safety padding inside parent (default: 48px).</param>
        public static float AutoFitTextToWidth(
            Label label,
            float maxFontSize = 64f,
            float minFontSize = 28f,
            float paddingBuffer = 48f)
        {
            if (label == null || string.IsNullOrEmpty(label.text))
                return maxFontSize;

            float targetWidth = 0f;
            var parent = label.parent;
            if (parent != null)
            {
                if (parent.contentRect.width > 0f)
                {
                    targetWidth = parent.contentRect.width - paddingBuffer;
                }
                else if (parent.resolvedStyle.width > 0f)
                {
                    float pad = parent.resolvedStyle.paddingLeft + parent.resolvedStyle.paddingRight;
                    targetWidth = parent.resolvedStyle.width - pad - paddingBuffer;
                }
                else if (parent.style.width.keyword == StyleKeyword.Undefined && parent.style.width.value.value > 0f)
                {
                    float pad = (parent.style.paddingLeft.keyword == StyleKeyword.Undefined ? parent.style.paddingLeft.value.value : 0f)
                              + (parent.style.paddingRight.keyword == StyleKeyword.Undefined ? parent.style.paddingRight.value.value : 0f);
                    targetWidth = parent.style.width.value.value - pad - paddingBuffer;
                }
            }

            // Fallback for standard modal card width (840px - 56px padding - 48px buffer = 736px)
            if (targetWidth <= 0f)
            {
                targetWidth = 736f;
            }

            label.style.fontSize = maxFontSize;
            Vector2 measured = label.MeasureTextSize(
                label.text,
                0,
                VisualElement.MeasureMode.Undefined,
                0,
                VisualElement.MeasureMode.Undefined);

            float fittedSize = maxFontSize;
            if (measured.x > targetWidth && targetWidth > 0f)
            {
                float ratio = targetWidth / measured.x;
                fittedSize = Mathf.Clamp(Mathf.Floor(maxFontSize * ratio), minFontSize, maxFontSize);
                label.style.fontSize = fittedSize;

                measured = label.MeasureTextSize(
                    label.text,
                    0,
                    VisualElement.MeasureMode.Undefined,
                    0,
                    VisualElement.MeasureMode.Undefined);

                while (measured.x > targetWidth && fittedSize > minFontSize)
                {
                    fittedSize -= 1f;
                    label.style.fontSize = fittedSize;
                    measured = label.MeasureTextSize(
                        label.text,
                        0,
                        VisualElement.MeasureMode.Undefined,
                        0,
                        VisualElement.MeasureMode.Undefined);
                }
            }
            else
            {
                label.style.fontSize = maxFontSize;
            }

            return fittedSize;
        }

        /// <summary>
        /// Registers geometry change callbacks on both the label and its parent container
        /// to ensure the label font size dynamically adapts whenever layouts or dimensions shift.
        /// </summary>
        public static void EnableAutoFit(
            Label label,
            float maxFontSize = 64f,
            float minFontSize = 28f,
            float paddingBuffer = 48f)
        {
            if (label == null) return;

            AutoFitTextToWidth(label, maxFontSize, minFontSize, paddingBuffer);

            label.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (Mathf.Abs(evt.oldRect.width - evt.newRect.width) > 1f)
                {
                    AutoFitTextToWidth(label, maxFontSize, minFontSize, paddingBuffer);
                }
            });

            if (label.parent != null)
            {
                label.parent.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    if (Mathf.Abs(evt.oldRect.width - evt.newRect.width) > 1f)
                    {
                        AutoFitTextToWidth(label, maxFontSize, minFontSize, paddingBuffer);
                    }
                });
            }
        }

        /// <summary>
        /// Smoothly fades out the current UI screen over a brief interval (allowing button punch/audio feedback to render)
        /// and loads the target scene asynchronously via LoadSceneAsync to avoid raw freeze frames on WebGL and iOS.
        /// </summary>
        public static Coroutine TransitionToSceneAsync(
            MonoBehaviour host,
            VisualElement root,
            string sceneName,
            float fadeOutDuration = 0.12f)
        {
            if (!Application.isPlaying || host == null || !host.gameObject.activeInHierarchy || root == null)
            {
                IsSceneTransitioning = false;
                PendingSceneFadeIn = false;
                SceneManager.LoadScene(sceneName);
                return null;
            }

            if (IsSceneTransitioning)
            {
                return null;
            }

            IsSceneTransitioning = true;
            PendingSceneFadeIn = true;
            return host.StartCoroutine(DoTransitionToSceneAsync(root, sceneName, fadeOutDuration));
        }

        private static IEnumerator DoTransitionToSceneAsync(VisualElement root, string sceneName, float fadeOutDuration)
        {
            VisualElement overlay = CreateTransitionOverlay(0f, PickingMode.Position);
            root.Add(overlay);

            float elapsed = 0f;
            float dur = Mathf.Max(0.01f, fadeOutDuration);
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.style.opacity = Mathf.Clamp01(elapsed / dur);
                yield return null;
            }

            overlay.style.opacity = 1f;
            // Render one full opaque frame before starting scene load
            yield return null;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            IsSceneTransitioning = false;
        }

        /// <summary>
        /// If arriving from TransitionToSceneAsync, holds an opaque dark veil for 1 rendered frame (masking frame-1 GPU PSO prewarm)
        /// and smoothly fades into the newly initialized scene.
        /// </summary>
        public static Coroutine PlayPendingSceneFadeIn(
            MonoBehaviour host,
            VisualElement root,
            float fadeInDuration = 0.18f)
        {
            IsSceneTransitioning = false;

            if (!PendingSceneFadeIn || !Application.isPlaying || host == null || !host.gameObject.activeInHierarchy || root == null)
            {
                PendingSceneFadeIn = false;
                return null;
            }

            PendingSceneFadeIn = false;
            return host.StartCoroutine(DoSceneFadeIn(root, fadeInDuration));
        }

        private static IEnumerator DoSceneFadeIn(VisualElement root, float fadeInDuration)
        {
            VisualElement overlay = CreateTransitionOverlay(1f, PickingMode.Ignore);
            root.Add(overlay);

            // Hold 1 rendered frame so targeted in-frustum VFX PSO warmup completes invisibly
            yield return null;

            float elapsed = 0f;
            float dur = Mathf.Max(0.01f, fadeInDuration);
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.style.opacity = 1f - Mathf.Clamp01(elapsed / dur);
                yield return null;
            }

            if (overlay.parent != null)
            {
                overlay.RemoveFromHierarchy();
            }
        }

        private static VisualElement CreateTransitionOverlay(float initialOpacity, PickingMode picking)
        {
            var overlay = new VisualElement { name = "scene-transition-overlay" };
            overlay.pickingMode = picking;
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.right = 0f;
            overlay.style.top = 0f;
            overlay.style.bottom = 0f;
            overlay.style.backgroundColor = new StyleColor(new Color(0.02f, 0.03f, 0.07f, 1f));
            overlay.style.opacity = initialOpacity;
            return overlay;
        }
    }
}
