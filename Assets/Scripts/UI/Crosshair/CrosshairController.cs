using MyToolz.Player.FPS.CombatSystem.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.View
{
    [System.Serializable]
    public abstract class Scope 
    {
        [SerializeField] protected bool hideWhenAim;
        [SerializeField] protected uint priority;
        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
        public abstract bool Show();
        public abstract bool Hide();

        public bool HideWhenAim() => hideWhenAim;

        public uint GetPriority() => priority;
    }

    [System.Serializable]
    public class CrosshairScope : Scope
    {
        public RectTransform CrosshairHolder;
        public CanvasGroup CrosshairLines;
        [SerializeField] private CrosshairController crosshairController;
        public ReticleSO ReticleSO;

        private Vector2 spread
        {
            get => crosshairController.Spread;
        }

        public override void Enter()
        {
            CrosshairLines.alpha = 1;
        }

        public override void Update()
        {
           CrosshairHolder.sizeDelta = Vector2.Lerp(CrosshairHolder.sizeDelta, spread * ReticleSO.DeltaSizeMultiplier, ReticleSO.RecoverSpeed * Time.deltaTime);
        }

        public override void Exit()
        {
            CrosshairLines.alpha = 0;
        }

        public override bool Show()
        {
            CrosshairLines.alpha = Mathf.Lerp(CrosshairLines.alpha, 1, Time.deltaTime * ReticleSO.FadeSpeed);
            return Mathf.Approximately(CrosshairLines.alpha, 1);
        }

        public override bool Hide()
        {
            CrosshairLines.alpha = Mathf.Lerp(CrosshairLines.alpha, 0, Time.deltaTime * ReticleSO.FadeSpeed);
            return Mathf.Approximately(CrosshairLines.alpha, 0);
        }
    }

    [System.Serializable]
    public class HitmarkerHeadshotScope : Scope
    {
        public CanvasGroup HitmarkerObject;
        public RectTransform RectTransform;
        public float FadeSpeed;
        public float FadeDelay;
        public float MaxSize;
        public float MinSize;
        public float RotationAmount;

        private float fadeTimer;

        public override void Enter()
        {
            fadeTimer = 1;
            HitmarkerObject.alpha = 1;
            RectTransform.sizeDelta = new Vector3(MaxSize, MaxSize, MaxSize);
            Vector3 rotation = new Vector3(0, 0, UnityEngine.Random.Range(-RotationAmount, RotationAmount));
            RectTransform.Rotate(rotation);
        }

        public override void Update()
        {
            if (HitmarkerObject.alpha == 0) return;
            Vector3 scale = new Vector3(MinSize, MinSize, MinSize);
            RectTransform.sizeDelta = Vector3.Lerp(RectTransform.sizeDelta, scale, Time.deltaTime * 30);
            RectTransform.rotation = Quaternion.Slerp(RectTransform.rotation, Quaternion.identity, Time.deltaTime * 8);
            if (fadeTimer > 0) fadeTimer -= Time.deltaTime;
            if (fadeTimer <= 0)
            {
                Hide();
            }
        }

        public override void Exit()
        {
            HitmarkerObject.alpha = 0;
            RectTransform.rotation = Quaternion.identity;
        }

        public override bool Show()
        {
            fadeTimer = FadeDelay;
            HitmarkerObject.alpha = Mathf.Lerp(HitmarkerObject.alpha, 1, Time.deltaTime * FadeSpeed);
            return Mathf.Approximately(HitmarkerObject.alpha, 1);
        }

        public override bool Hide()
        {
            HitmarkerObject.alpha = Mathf.Lerp(HitmarkerObject.alpha, 0, Time.deltaTime * FadeSpeed);
            return Mathf.Approximately(HitmarkerObject.alpha, 0);
        }
    }

    [System.Serializable]
    public class HitmarkerKillScope : HitmarkerHeadshotScope
    {

    }

    [System.Serializable]
    public class HitmarkerHeadshotKillScope : HitmarkerHeadshotScope
    {

    }

    [System.Serializable]
    public class HitmarkerScope : HitmarkerHeadshotScope
    {

    }

    [System.Serializable]
    public class ScopeWrapper
    {
       [SerializeReference, SubclassSelector] public Scope Scope;
    }

    public class CrosshairController : MonoBehaviour
    {
        [SerializeField] private ScopeWrapper[] scopes;
        private Dictionary<Type, Scope> scopeMap = new Dictionary<Type, Scope>();
        private Dictionary<uint, Scope> activeMarkers = new Dictionary<uint, Scope>();
        private Coroutine fadeCoroutine;
        public Vector2 Spread = Vector2.one;

        private void Awake()
        {
            foreach (var scope in scopes)
            {
                scopeMap[scope.Scope.GetType()] = scope.Scope;
            }
            ChangeState(typeof(CrosshairScope));
        }

        public void UpdateCrosshairSize(Vector2 spreadAmount)
        {
           Spread = spreadAmount;
        }
        private void Update()
        {
            foreach (var marker in activeMarkers.Values)
            {
                marker.Update();
            }
        }

        public void Show()
        {
            if (gameObject == null) return;
            if (!gameObject.activeInHierarchy) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInMarker());
        }

        public void Hide()
        {
            if (gameObject == null) return;
            if (!gameObject.activeInHierarchy) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutMarker());
        }

        private IEnumerator FadeInMarker()
        {
            bool allShown;
            do
            {
                allShown = true;
                foreach (var marker in activeMarkers.Values)
                {
                    if (marker.HideWhenAim() && !marker.Show())
                        allShown = false;
                }
                yield return null;
            } while (!allShown);
        }

        private IEnumerator FadeOutMarker()
        {
            bool allHidden;
            do
            {
                allHidden = true;
                foreach (var marker in activeMarkers.Values)
                {
                    if (marker.HideWhenAim() && !marker.Hide())
                        allHidden = false;
                }
                yield return null;
            } while (!allHidden);
        }

        public void ChangeState(Type markerType)
        {
            if (!typeof(Scope).IsAssignableFrom(markerType)) return;
            if (!scopeMap.TryGetValue(markerType, out var newState)) return;
            if (activeMarkers.TryGetValue(newState.GetPriority(),out var currentState))
            {
                currentState.Exit();
            }
            activeMarkers[newState.GetPriority()] = newState;
            currentState = newState;
            currentState.Enter();
        }

    }   
}