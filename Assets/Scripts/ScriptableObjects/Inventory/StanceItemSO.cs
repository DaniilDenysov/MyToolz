using MyToolz.EditorToolz;
using MyToolz.Player.Platformer.Combo;
using MyToolz.ScriptableObjects.Player.Platformer.Combat;
using MyToolz.ScriptableObjects.Player.Platformer.Movement;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Inventory
{
    [CreateAssetMenu(fileName = "StanceItemSO", menuName = "MyToolz/Inventory/StanceItemSO")]
    public class StanceItemSO : ItemSO
    {
        [FoldoutGroup("Stance"), SerializeField] protected WeaponSO weaponSO;
        [FoldoutGroup("Stance"), SerializeField] protected PlayerMovementStats playerMovementStats;
        [FoldoutGroup("Stance"), SerializeField] protected MovementStateSO [] movementStates; 
        [FoldoutGroup("Stance"), SerializeField] protected PlayerAttackSO [] attackStates; 
        [FoldoutGroup("Stance"), SerializeField] protected ComboSO [] combos; 
        public MovementStateSO [] MovementStates => movementStates;
        public PlayerAttackSO[] AattackStates => attackStates;
        public WeaponSO WeaponSO => weaponSO;
        public IReadOnlyList<ComboSO> Combos => combos;
        public PlayerMovementStats Stats => playerMovementStats;
    }
}
