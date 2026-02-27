using UnityEngine;

namespace MyToolz.FreeCamera
{
    [CreateAssetMenu(fileName = "FreeCameraSO", menuName = "MyToolz/FreeCamera/FreeCameraSO")]
    public class FreeCameraSO : ScriptableObject
    {
        [Header("Speed")]
        [SerializeField, Range(0.1f, 100f)] private float moveSpeed = 10f;
        [SerializeField, Range(0.1f, 100f)] private float minSpeed = 0.5f;
        [SerializeField, Range(0.1f, 100f)] private float maxSpeed = 50f;
        [SerializeField, Range(1f, 20f)] private float boostMultiplier = 3f;

        [Header("Look")]
        [SerializeField, Range(1f, 1000f)] private float lookSensitivity = 200f;

        [Header("Scroll")]
        [SerializeField, Range(0.01f, 10f)] private float scrollSensitivity = 0.5f;

        [Header("Smoothing")]
        [SerializeField, Range(0f, 1f)] private float moveSmoothTime = 0.15f;
        [SerializeField, Range(0f, 1f)] private float lookSmoothTime = 0.05f;

        public float MoveSpeed => moveSpeed;
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public float BoostMultiplier => boostMultiplier;
        public float LookSensitivity => lookSensitivity;
        public float ScrollSensitivity => scrollSensitivity;
        public float MoveSmoothTime => moveSmoothTime;
        public float LookSmoothTime => lookSmoothTime;
    }
}
