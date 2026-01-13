using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Animator simple para UI Image con frames de idle y ataque.
    /// Reproduce en bucle idle y un ataque una sola vez, luego vuelve a idle.
    /// Seguro si faltan frames (no crashea).
    /// </summary>
    public class SpriteFrameAnimatorUI : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private List<Sprite> idleFrames = new List<Sprite>();
        [SerializeField] private List<Sprite> attackFrames = new List<Sprite>();
        [SerializeField] private float fps = 16f;

        private Coroutine _currentRoutine;

        public void Configure(Image target, List<Sprite> idle, List<Sprite> attack, float framesPerSecond)
        {
            targetImage = target;
            idleFrames = idle ?? new List<Sprite>();
            attackFrames = attack ?? new List<Sprite>();
            fps = framesPerSecond > 0 ? framesPerSecond : 16f;
        }

        public void PlayIdleLoop()
        {
            if (idleFrames == null || idleFrames.Count == 0 || targetImage == null)
            {
                return;
            }

            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
            }

            _currentRoutine = StartCoroutine(IdleLoop());
        }

        /// <summary>
        /// Reproduce ataque una sola vez. Devuelve duración estimada en segundos.
        /// Al finalizar, vuelve a idle si hay frames.
        /// </summary>
        public float PlayAttackOnce(Action onComplete = null)
        {
            if (targetImage == null || attackFrames == null || attackFrames.Count == 0 || fps <= 0)
            {
                onComplete?.Invoke();
                PlayIdleLoop();
                return 0f;
            }

            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
            }

            float duration = attackFrames.Count / fps;
            _currentRoutine = StartCoroutine(AttackRoutine(onComplete));
            return duration;
        }

        private IEnumerator IdleLoop()
        {
            int index = 0;
            float frameTime = 1f / Mathf.Max(1f, fps);
            while (true)
            {
                if (targetImage != null && idleFrames != null && idleFrames.Count > 0)
                {
                    targetImage.sprite = idleFrames[index % idleFrames.Count];
                }
                index++;
                yield return new WaitForSeconds(frameTime);
            }
        }

        private IEnumerator AttackRoutine(Action onComplete)
        {
            float frameTime = 1f / Mathf.Max(1f, fps);
            if (attackFrames != null && targetImage != null)
            {
                for (int i = 0; i < attackFrames.Count; i++)
                {
                    targetImage.sprite = attackFrames[i];
                    yield return new WaitForSeconds(frameTime);
                }
            }

            onComplete?.Invoke();
            PlayIdleLoop();
        }
    }
}
