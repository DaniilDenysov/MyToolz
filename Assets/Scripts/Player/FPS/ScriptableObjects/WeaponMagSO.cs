using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "Mag", menuName = "create new mag")]
    public class WeaponMagSO : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        public GameObject Prefab { get => prefab; private set { } }
        [SerializeField, Range(0, 1000)] private int maxBullets;
        public int GetMaxBullets() => maxBullets;
        [SerializeField, Range(0, 1000)] private float reloadTime;
        public float GetReloadTime() => reloadTime;
    }
}
