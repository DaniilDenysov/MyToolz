using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class MergeTweenStrategy : TweenStrategy
    {
        //Only parent trigger is taken into account!
        [SerializeField]
        private bool parallel = true;

        [SerializeReference]
        private TweenStrategy[] innerStrategies;

        public override Tween GetTween()
        {
            if (innerStrategies == null || innerStrategies.Length == 0)
            {
                DebugUtility.LogWarning(this, "MergeTweenStrategy: No inner strategies assigned.");
                return null;
            }

            if (parallel)
            {
                var sequence = DOTween.Sequence();

                foreach (var strategy in innerStrategies)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Join(tween);
                    }
                }

                return sequence;
            }
            else
            {
                var sequence = DOTween.Sequence();

                foreach (var strategy in innerStrategies)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Append(tween);
                    }
                }

                return sequence;
            }
        }
    }
}