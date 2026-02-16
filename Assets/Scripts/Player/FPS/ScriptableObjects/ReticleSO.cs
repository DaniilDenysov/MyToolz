using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "create new reticle", menuName = "NoSaints/CombatSystem/Reticle")]
    public class ReticleSO : ScriptableObject
    {
        [SerializeField, Range(1f, 1000f)] private float deltaSizeMultiplier = 14f;
        public float DeltaSizeMultiplier { get { return deltaSizeMultiplier; } }
        [SerializeField, Range(1f, 1000f)] private float smoothness = 5f;
        public float Smoothness { get { return smoothness; } }
        [SerializeField, Range(1f, 1000f)] private float fadeSpeed = 10f;
        public float FadeSpeed {  get { return fadeSpeed; } }

        [SerializeField, Range(1f, 1000f)] private float recoverSpeed = 10f;
        public float RecoverSpeed { get { return recoverSpeed; } }

    }
}
