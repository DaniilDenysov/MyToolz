using System.Collections;
using UnityEngine;

namespace MyToolz.Player.FPS.DisposableObjects
{
    public abstract class DisposableObject : MonoBehaviour
    {
        [SerializeField] protected int disposeDelay = 10;
        [SerializeField] protected float disposeSpeed = 10f, threshold = 5f;
        protected Vector3 initialSize;
        private Coroutine coroutine;

        public virtual void Awake()
        {
            initialSize = transform.localScale;
        }

        public virtual void Start()
        {
            StartShrinking();
        }

        public virtual void OnStartDisposing()
        {

        }

        public virtual void ResetObject()
        {
            transform.localScale = initialSize;
        }

        public void StartShrinking()
        {
            if (gameObject.activeInHierarchy == false) return;
            coroutine = StartCoroutine(Dispose(disposeSpeed));
        }

        public IEnumerator Dispose(float disposeSpeed)
        {
            OnStartDisposing();
            yield return new WaitForSeconds(disposeDelay);

            while (ShouldBeShrinken())
            {
                Shrink();
                yield return null;
            }

            OnObjectDispose();
        }

        public virtual void OnDestroy()
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }

        public abstract void OnObjectDispose();

        public virtual bool ShouldBeShrinken() => (transform != null && transform.localScale.sqrMagnitude > threshold * threshold);
        public virtual void Shrink()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, disposeSpeed * Time.deltaTime);
        }
    }
}