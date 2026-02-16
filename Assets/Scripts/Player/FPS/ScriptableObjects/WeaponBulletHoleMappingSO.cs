using MyToolz.Networking.Utilities;
using MyToolz.Player.FPS.DisposableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Networking.Utilities
{
    public static class MappingUtility
    {
        public static string PhysicsMaterialToName(PhysicMaterial physicMaterial)
        {
            return (physicMaterial == null || string.IsNullOrEmpty(physicMaterial.name) ||
            string.IsNullOrWhiteSpace(physicMaterial.name)) ?
            "" :
            physicMaterial.name;
        }
    }
}

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "WeaponBulletHoleVFXMapping", menuName = "NoSaints/CombatSystem/Weapon Bullet Hole VFX")]
    public class WeaponBulletHoleMappingSO : ScriptableObject
    {
        [System.Serializable]
        public struct BulletHoleMapping
        {
            public PhysicMaterial physicMaterial;
            public DisposableBulletHole bulletHolePrefab;
        }

        [SerializeField] private List<BulletHoleMapping> mappings;

        private Dictionary<string, DisposableBulletHole> dictionaryMapping;

        private void InitializeMapping()
        {
            dictionaryMapping = new Dictionary<string, DisposableBulletHole>();
            foreach (var mapping in mappings)
            {
                if (mapping.bulletHolePrefab == null) continue;
                string materialName = MappingUtility.PhysicsMaterialToName(mapping.physicMaterial);
                dictionaryMapping.TryAdd(materialName, mapping.bulletHolePrefab);
            }
        }

        public DisposableBulletHole GetBulletHolePrefab(string materialName)
        {
            if (dictionaryMapping == null)
            {
                InitializeMapping();
            }
            if (!dictionaryMapping.TryGetValue(materialName, out var bulletHole)) return null;
            return bulletHole;
        }
    }
}
