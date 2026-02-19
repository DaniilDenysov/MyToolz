using DG.Tweening;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class SpriteSwapTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] private Sprite newSprite;
        [SerializeField, Required] private Image spriteRenderer;
        [SerializeField] private bool fade;
        [SerializeField, ShowIf("@fade")] private float fadeDuration = 0.2f;

        public override Tween GetTween()
        {
            if (spriteRenderer == null || newSprite == null)
            {
                DebugUtility.LogError(this, "SpriteSwapTweenStrategy: SpriteRenderer or NewSprite is null.");
                return null;
            }

            Sequence sequence = DOTween.Sequence();
            if (fade)
            {
                sequence.Append(spriteRenderer.DOFade(0f, fadeDuration))
                        .AppendCallback(() =>
                        {
                            spriteRenderer.sprite = newSprite;
                        })
                        .Append(spriteRenderer.DOFade(1f, fadeDuration));
            }
            else
            {
                sequence.AppendCallback(() =>
                {
                    spriteRenderer.sprite = newSprite;
                });
            }
            return sequence;
        }
    }
}