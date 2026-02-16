using Sirenix.OdinInspector;
using MyToolz.DataStructures;
using MyToolz.Math;
using MyToolz.Pathfinding;
using UnityEngine;

namespace MyToolz.ScriptableObjects
{
    [CreateAssetMenu(fileName = "MapSO", menuName = "TestAssignment/MapSO")]
    public class MapSO : ScriptableObject
    {
        [SerializeField, ReadOnly] public float TileRadius = 1f;
        //[SerializeField, ReadOnly] public PositionToMapTileDictionary PositionToMapTileDictionary;
        //public bool TryGetTileAt(CubicVector3 position, out MapTile2DMatrix mapTile)
        //{
        //    return PositionToMapTileDictionary.TryGetValue(position,out mapTile);
        //}
    }
}
