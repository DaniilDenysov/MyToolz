using MyToolz.InventorySystem.Models;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    public class ThrowableSO : ItemSO
    {
        [SerializeField, Range(0, 30)] private float mass;
        public float Mass { get => mass; private set { } }

        [SerializeField, Range(0, 1000)] private float throwForce;
        public float ThrowForce { get => throwForce; private set { } }

        [Header("Prefab")]
        [SerializeField] private GameObject prefab;
        public GameObject Prefab { get => prefab; private set { } }
    }
}
