using DG.Tweening;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Tweener
{
    [System.Serializable]
    public abstract class AbstractTweenStrategy
    {
        public abstract Tween GetTween();
    }

    public abstract class Tweener<T> : MonoBehaviour where T : AbstractTweenStrategy
    {

        [SerializeField]
        protected bool paralelExecution;

        [SerializeField]
        [Tooltip("When enabled, tweens run on unscaled time and ignore any Time.timeScale changes (pauses, slow-motion, etc.).")]
        protected bool ignoreTimeScale = true;

        [SerializeReference] protected T[] tweenStrategies;

        protected List<Tween> runningTweens = new List<Tween>();


        public virtual Tween CreateSequence(List<T> strategiesList)
        {
            if (strategiesList == null || strategiesList.Count == 0)
                return null;

            Sequence sequence = DOTween.Sequence();

            foreach (var strategy in strategiesList)
            {
                var tween = strategy?.GetTween();
                if (tween == null)
                    continue;

                if (paralelExecution)
                    sequence.Join(tween);
                else
                    sequence.Append(tween);
            }

            ApplyTimeMode(sequence);
            Track(sequence);
            return sequence;
        }

        /// <summary>
        /// Registers a sequence owned by this tweener: it is killed with the GameObject and forgotten
        /// once finished, so the running list does not grow for the lifetime of the component.
        /// </summary>
        protected void Track(Tween tween)
        {
            if (tween == null)
                return;

            runningTweens.RemoveAll(t => !t.IsActive());
            tween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            runningTweens.Add(tween);
        }

        /// <summary>True while any sequence started by this tweener is still running.</summary>
        public bool IsTweening
        {
            get
            {
                foreach (var tween in runningTweens)
                {
                    if (tween.IsActive() && tween.IsPlaying())
                        return true;
                }

                return false;
            }
        }

        protected virtual void OnDestroy()
        {
            CancelSequence();
        }

        /// <summary>
        /// Applies the current time mode to a sequence. When <see cref="ignoreTimeScale"/> is
        /// true the sequence updates on unscaled time, so it keeps playing regardless of
        /// <see cref="UnityEngine.Time.timeScale"/> (pauses, slow-motion, etc.).
        /// </summary>
        protected Sequence ApplyTimeMode(Sequence sequence)
        {
            sequence?.SetUpdate(ignoreTimeScale);
            return sequence;
        }

        /// <summary>
        /// Toggles whether tweens ignore <see cref="UnityEngine.Time.timeScale"/> at runtime.
        /// Applies to future sequences as well as any that are already running.
        /// </summary>
        public void SetIgnoreTimeScale(bool ignore)
        {
            ignoreTimeScale = ignore;

            foreach (var tween in runningTweens)
            {
                if (tween.IsActive())
                    tween.SetUpdate(ignoreTimeScale);
            }
        }

        protected void CancelSequence()
        {
            foreach (var tween in runningTweens)
            {
                if (tween.IsActive())
                {
                    tween.Kill();
                }
            }

            runningTweens.Clear();
        }
    }
}
