using UnityEngine;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Kit/UI Spinner")]
    public class UISpinner : MonoBehaviour
    {
        [Tooltip("Degrees per second. Negative spins clockwise.")]
        [SerializeField] private float speed = -220f;

        [Tooltip("Steps per full turn. 0 spins smoothly; 8 or 12 gives the clicky mechanical feel.")]
        [SerializeField, Range(0, 24)] private int steps;

        private RectTransform target;
        private float angle;

        private void Awake() => target = (RectTransform)transform;

        private void OnEnable() => angle = 0f;

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            angle += speed * Time.unscaledDeltaTime;
            angle %= 360f;

            float applied = steps > 0
                ? Mathf.Floor(angle / (360f / steps)) * (360f / steps)
                : angle;

            target.localRotation = Quaternion.Euler(0f, 0f, applied);
        }
    }
}
