using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using MyToolz.Core;

namespace MyToolz.Tweener
{
    [System.Serializable]
    public abstract class AbstractTweenStrategy : ObjectPlus
    {
        public abstract Tween GetTween();
    }

    public abstract class Tweener<T> : MonoBehaviourPlus where T : AbstractTweenStrategy
    {

        [SerializeField]
        protected bool paralelExecution;

        [SerializeReference] protected T[] tweenStrategies;

        protected List<Tween> runningTweens = new List<Tween>();


        public virtual Tween CreateSequence(List<T> strategiesList)
        {
            if (strategiesList == null || strategiesList.Count == 0)
                return null;

            if (paralelExecution)
            {
                Sequence sequence = DOTween.Sequence();
                foreach (var strategy in strategiesList)
                {
                    var tween = strategy?.GetTween();
                    if (tween != null)
                    {
                        sequence.Join(tween);
                        tween.OnComplete(() =>
                        {

                        })
                        .OnKill(() =>
                        {

                        });
                        tween.Play();
                    }
                }
                runningTweens.Add(sequence);
                return sequence;
            }
            else
            {
                Sequence sequence = DOTween.Sequence();

                foreach (var strategy in strategiesList)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Append(tween);
                    }
                }
                sequence.OnComplete(() =>
                {

                })
                .OnKill(() =>
                {

                });
                runningTweens.Add(sequence);
                return sequence;
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
            Log("UITweener tweens cancelled.");
        }
    }
}