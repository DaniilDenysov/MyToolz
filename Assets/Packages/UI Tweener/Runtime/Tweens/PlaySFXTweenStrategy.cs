// NOTE: Commented out — depends on MyToolz.ScriptableObjects.Audio which is not part of this package set.
// Uncomment when the Audio package is available.
//
// using DG.Tweening;
// using MyToolz.EditorToolz;
// using MyToolz.Extensions;
// using MyToolz.ScriptableObjects.Audio;
// using UnityEngine;
//
// namespace MyToolz.Tweener.UI.Tweens
// {
//     [System.Serializable]
//     public class PlaySFXTweenStrategy : TweenStrategy
//     {
//         [SerializeField, Required] private AudioClipSO audioClip;
//         [SerializeField, Required] private AudioSource audioSource;
//
//         public override Tween GetTween()
//         {
//             return DOTween.Sequence().OnComplete(() =>
//             {
//                 audioSource.Play(audioClip);
//             });
//         }
//     }
// }