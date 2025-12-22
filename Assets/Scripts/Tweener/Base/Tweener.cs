using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using MyToolz.Utilities.Debug;

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

        [SerializeReference] protected T [] tweenStrategies;

        protected List<Tween> runningTweens = new List<Tween>();


        public virtual void ExecuteSequence(List<T> strategiesList)
        {
            if (strategiesList == null || strategiesList.Count == 0)
                return;

            if (paralelExecution)
            {
                int tweensToComplete = 0;
                int tweensCompleted = 0;

                foreach (var strategy in strategiesList)
                {
                    var tween = strategy?.GetTween();
                    if (tween != null)
                    {
                        tweensToComplete++;
                        runningTweens.Add(tween);

                        tween.OnComplete(() =>
                        {
                            tweensCompleted++;
                        })
                        .OnKill(() =>
                        {

                        });

                        tween.Play();
                    }
                }

                if (tweensToComplete == 0)
                {

                }
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
                        runningTweens.Add(tween);
                    }
                }
                sequence.OnComplete(() =>
                {

                })
                .OnKill(() =>
                {

                });

                sequence.Play();
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
            DebugUtility.Log("UITweener tweens cancelled.");
        }
    }
}