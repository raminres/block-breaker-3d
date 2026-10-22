using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        private static readonly Dictionary<VisualElement, Coroutine> activeCoroutines = new Dictionary<VisualElement, Coroutine>();

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

        public static void StopActiveAnimation(MonoBehaviour host, VisualElement element)
        {
            if (element != null && activeCoroutines.TryGetValue(element, out var running) && running != null)
            {
                if (host != null) host.StopCoroutine(running);
                activeCoroutines.Remove(element);
            }
        }
    }
}
