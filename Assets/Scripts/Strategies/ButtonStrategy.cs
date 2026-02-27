using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;
using System;
using MyToolz.InputManagement.Commands;

namespace MyToolz.Strategies
{
    [System.Serializable]
    public abstract class ButtonStrategy
    {
        public Action OnFired;
        public Action OnStarted;
        public Action OnCanceled;
        [SerializeField] protected InputCommandSO interactInputCommandSO;
        [SerializeField] private GameObject body;

        public virtual void Enable()
        {
            if (body) body.SetActive(true);
            OnEnable();
        }

        public virtual void Disable()
        {
            if (body) body.SetActive(false);
            OnDisable();
        }

        public abstract void OnEnable();
        public virtual void Update() { }
        public abstract void OnDisable();
    }

    [System.Serializable]
    public class HoldButtonStrategy : ButtonStrategy
    {
        [SerializeField] private Image image;
        [SerializeField] private float fillDuration = 2f;
        private Tween fillTween;

        public override void OnEnable()
        {
            interactInputCommandSO.OnInputStarted += OnInteractionStarted;
            interactInputCommandSO.OnInputCanceled += OnInteractionCanceled;
        }

        public override void OnDisable()
        {
            interactInputCommandSO.OnInputStarted -= OnInteractionStarted;
            interactInputCommandSO.OnInputCanceled -= OnInteractionCanceled;
            KillTween();
            image.fillAmount = 0f;
        }

        private void OnInteractionStarted(InputCommandSO context)
        {
            //if (context.interaction is not UnityEngine.InputSystem.Interactions.HoldInteraction) return;
            KillTween();
            OnStarted?.Invoke();
            float remaining = 1f - image.fillAmount;
            fillTween = image.DOFillAmount(1f, fillDuration * remaining)
                             .SetEase(Ease.Linear)
                             .OnComplete(() =>
                             {
                                 OnFired?.Invoke();
                                 image.fillAmount = 0f;
                             });
        }

        private void OnInteractionCanceled(InputCommandSO context)
        {
            //if (context.interaction is not UnityEngine.InputSystem.Interactions.HoldInteraction) return;
            OnCanceled?.Invoke();
            KillTween();
            image.fillAmount = 0f;
        }

        private void KillTween()
        {
            if (fillTween != null && fillTween.IsActive())
                fillTween.Kill();
        }
    }

    [System.Serializable]
    public class PressButtonStrategy : ButtonStrategy
    {
        [SerializeField] private Image image;
        [SerializeField] private float pressDuration = 0.2f;
        [SerializeField] private float scaleMultiplier = 1.1f;

        private Vector3 originalScale;
        private bool initialized = false;

        public override void OnEnable()
        {
            interactInputCommandSO.OnInputPerformed += OnPressed;

            if (!initialized && image != null)
            {
                originalScale = image.transform.localScale;
                initialized = true;
            }
        }

        public override void OnDisable()
        {
            interactInputCommandSO.OnInputPerformed -= OnPressed;
            image.transform.localScale = originalScale;
        }

        private void OnPressed(InputCommandSO context)
        {
            //if (context.interaction is not UnityEngine.InputSystem.Interactions.PressInteraction) return;
            if (image == null) return;

            image.transform.DOKill();
            image.transform.localScale = originalScale;

            image.transform.DOScale(originalScale * scaleMultiplier, pressDuration / 2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    image.transform.DOScale(originalScale, pressDuration / 2f)
                        .SetEase(Ease.InBack);
                });

            OnFired?.Invoke();
        }
    }
}