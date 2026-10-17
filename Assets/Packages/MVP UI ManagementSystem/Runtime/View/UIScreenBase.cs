using MyToolz.Tweener.UI;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyToolz.UI.Management
{
    public abstract class UIScreenBase : MonoBehaviour, IUIState
    {
        [Header("Base UI Config")]
        [SerializeField] protected UIScreen parent;
        [SerializeField] protected UITweener screenTweener;
        [SerializeField] protected GameObject firstSelected;
        [Header("Callbacks")]
        [SerializeField] private UnityEvent onEnter;
        [SerializeField] private UnityEvent onExit;
        protected bool isActive;
        public bool IsActive => isActive;

        public UnityEvent OnEnterEvent => onEnter;
        public UnityEvent OnExitEvent => onExit;

        public virtual void OnEnter()
        {
            isActive = true;
            if (screenTweener != null)
            {
                screenTweener.SetActive(true);
            }

            ApplyFocus();

            onEnter?.Invoke();
        }

        public virtual void OnExit()
        {
            isActive = false;
            if (screenTweener != null)
            {
                screenTweener.SetActive(false);
            }

            ReleaseFocus();

            onExit?.Invoke();
        }

        /// <summary>
        /// Moves keyboard/controller focus to <see cref="firstSelected"/>. Setting only
        /// EventSystem.firstSelectedGameObject does not move an existing selection, so a screen opened
        /// after the first one would otherwise keep focus on the previous screen.
        /// </summary>
        protected virtual void ApplyFocus()
        {
            EventSystem eventSystem = EventSystem.current;
            if (firstSelected == null || eventSystem == null)
            {
                return;
            }

            eventSystem.firstSelectedGameObject = firstSelected;
            if (firstSelected.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(firstSelected);
            }
        }

        /// <summary>Clears the selection when it belongs to this screen, so focus does not stay on a hidden control.</summary>
        protected virtual void ReleaseFocus()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (firstSelected != null && eventSystem.firstSelectedGameObject == firstSelected)
            {
                eventSystem.firstSelectedGameObject = null;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        public override string ToString()
        {
            return $"[UIScreen] {gameObject.name}";
        }

        public abstract void Open();

        public abstract void Close();
    }
}
