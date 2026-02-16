using MyToolz.Core;
using UnityEngine;

namespace MyToolz
{
    [CreateAssetMenu(fileName = "TerrainTileSO", menuName = "MyToolz/TerrainTileSO")]
    public class TerrainTileSO : ScriptableObjectPlus
    {
        public bool IsWalkable = true;

        [SerializeField] private GameObject[] variants;

        public GameObject[] Variants => variants;

        public int TerrainCost;
    }
}