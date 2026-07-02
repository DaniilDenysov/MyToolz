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

            runningTweens.Add(sequence);
            return sequence;
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
            DebugUtility.Log(this, "UITweener tweens cancelled.");
        }
    }
}