using UnityEngine;
using DG.Tweening;

namespace RoguelikeCardBattler.Core.UI
{
    /// <summary>
    /// Stateless utility class with reusable UI animation methods built on DOTween.
    /// Every method uses the generic <c>DOTween.To()</c> API (the DOTween UI module
    /// is NOT enabled in this project, so extension methods like DOFade/DOScale are
    /// unavailable).
    ///
    /// All tweens use <c>SetUpdate(true)</c> so they run even when
    /// <c>Time.timeScale == 0</c> (future-proof for pause menus).
    /// All tweens call <c>SetTarget()</c> so they can be killed later
    /// with <c>DOTween.Kill(target)</c>.
    /// </summary>
    public static class UIAnimationHelper
    {
        /// <summary>
        /// Fades a CanvasGroup's alpha from its current value to 1 (fully visible).
        /// <example>
        /// <code>UIAnimationHelper.FadeIn(panelGroup, 0.3f);</code>
        /// </example>
        /// </summary>
        /// <param name="group">The CanvasGroup to fade in.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="ease">Easing curve (default: OutQuad for a natural deceleration).</param>
        /// <returns>The tween, for chaining <c>.OnComplete()</c>, <c>.SetDelay()</c>, etc.</returns>
        public static Tween FadeIn(CanvasGroup group, float duration, Ease ease = Ease.OutQuad)
        {
            return DOTween.To(() => group.alpha, x => group.alpha = x, 1f, duration)
                .SetTarget(group)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Fades a CanvasGroup's alpha from its current value to 0 (fully transparent).
        /// <example>
        /// <code>UIAnimationHelper.FadeOut(panelGroup, 0.2f);</code>
        /// </example>
        /// </summary>
        /// <param name="group">The CanvasGroup to fade out.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="ease">Easing curve (default: InQuad for a natural acceleration).</param>
        /// <returns>The tween, for chaining.</returns>
        public static Tween FadeOut(CanvasGroup group, float duration, Ease ease = Ease.InQuad)
        {
            return DOTween.To(() => group.alpha, x => group.alpha = x, 0f, duration)
                .SetTarget(group)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Immediately sets the target's scale to zero, then animates it to <c>Vector3.one</c>.
        /// The default <c>OutBack</c> ease gives a satisfying overshoot-bounce.
        /// <example>
        /// <code>UIAnimationHelper.ScaleIn(nodeTransform, 0.3f);</code>
        /// </example>
        /// </summary>
        /// <param name="target">Transform to scale in.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="ease">Easing curve (default: OutBack for a bounce effect).</param>
        /// <returns>The tween, for chaining.</returns>
        public static Tween ScaleIn(Transform target, float duration, Ease ease = Ease.OutBack)
        {
            target.localScale = Vector3.zero;
            return DOTween.To(() => target.localScale, x => target.localScale = x, Vector3.one, duration)
                .SetTarget(target)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Animates the target's scale from its current value down to <c>Vector3.zero</c>.
        /// <example>
        /// <code>UIAnimationHelper.ScaleOut(cardTransform, 0.15f);</code>
        /// </example>
        /// </summary>
        /// <param name="target">Transform to scale out.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="ease">Easing curve (default: InBack for a pull-in effect).</param>
        /// <returns>The tween, for chaining.</returns>
        public static Tween ScaleOut(Transform target, float duration, Ease ease = Ease.InBack)
        {
            return DOTween.To(() => target.localScale, x => target.localScale = x, Vector3.zero, duration)
                .SetTarget(target)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Starts an infinite yoyo loop that oscillates the target's scale between
        /// <c>Vector3.one</c> and <c>Vector3.one * scaleAmount</c>.
        /// Use <see cref="StopPulse"/> to kill the loop and reset scale.
        /// <example>
        /// <code>UIAnimationHelper.PulseLoop(availableNode.transform);</code>
        /// </example>
        /// </summary>
        /// <param name="target">Transform to pulse.</param>
        /// <param name="scaleAmount">Peak scale factor (default: 1.05 = 5 % larger).</param>
        /// <param name="duration">Duration of one half-cycle in seconds.</param>
        /// <returns>The tween (infinite loop). Kill it via <see cref="StopPulse"/> or <c>DOTween.Kill(target)</c>.</returns>
        public static Tween PulseLoop(Transform target, float scaleAmount = 1.05f, float duration = 0.6f)
        {
            target.localScale = Vector3.one;
            Vector3 peak = new Vector3(scaleAmount, scaleAmount, scaleAmount);
            return DOTween.To(() => target.localScale, x => target.localScale = x, peak, duration)
                .SetTarget(target)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /// <summary>
        /// Kills all tweens on the target and resets its scale to <c>Vector3.one</c>.
        /// Companion to <see cref="PulseLoop"/>.
        /// <example>
        /// <code>UIAnimationHelper.StopPulse(completedNode.transform);</code>
        /// </example>
        /// </summary>
        /// <param name="target">Transform whose pulse should stop.</param>
        public static void StopPulse(Transform target)
        {
            DOTween.Kill(target);
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Slides a RectTransform into its current position from an offset.
        /// The target is instantly moved to <c>anchoredPosition + fromOffset</c>,
        /// then animated back to its original anchoredPosition.
        /// <example>
        /// <code>
        /// // Slide a button up from 100px below its resting position
        /// UIAnimationHelper.SlideIn(buttonRect, new Vector2(0, -100f), 0.3f);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="target">RectTransform to slide.</param>
        /// <param name="fromOffset">Offset from the resting position where the animation starts.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="ease">Easing curve (default: OutQuad).</param>
        /// <returns>The tween, for chaining.</returns>
        public static Tween SlideIn(RectTransform target, Vector2 fromOffset, float duration, Ease ease = Ease.OutQuad)
        {
            Vector2 destination = target.anchoredPosition;
            target.anchoredPosition = destination + fromOffset;
            return DOTween.To(
                    () => target.anchoredPosition,
                    x => target.anchoredPosition = x,
                    destination,
                    duration)
                .SetTarget(target)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        /// Quick scale punch: briefly inflates the target's scale by <paramref name="force"/>
        /// then returns to the original scale with an elastic feel.
        /// Useful for hit/damage feedback on avatars or buttons.
        /// <example>
        /// <code>UIAnimationHelper.Punch(enemyAvatar, 0.2f);</code>
        /// </example>
        /// </summary>
        /// <param name="target">Transform to punch.</param>
        /// <param name="force">How much larger than 1.0 the scale peaks (default: 0.15 = 15 %).</param>
        /// <param name="duration">Total duration of the punch animation.</param>
        /// <returns>The tween, for chaining.</returns>
        public static Tween Punch(Transform target, float force = 0.15f, float duration = 0.3f)
        {
            Vector3 original = target.localScale;
            Vector3 peak = original * (1f + force);
            float halfDuration = duration * 0.35f;

            Sequence seq = DOTween.Sequence()
                .SetTarget(target)
                .SetUpdate(true);

            seq.Append(
                DOTween.To(() => target.localScale, x => target.localScale = x, peak, halfDuration)
                    .SetEase(Ease.OutQuad));
            seq.Append(
                DOTween.To(() => target.localScale, x => target.localScale = x, original, duration - halfDuration)
                    .SetEase(Ease.OutElastic));

            return seq;
        }
    }
}
