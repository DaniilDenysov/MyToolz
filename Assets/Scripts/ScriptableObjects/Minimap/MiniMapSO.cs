using MyToolz.EditorToolz;
using MyToolz.MiniMap;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.ScriptableObjects.MiniMap
{
    [CreateAssetMenu(fileName = "Minimap", menuName = "MyToolz/Minimap")]
    public class MiniMapSO : ScriptableObject
    {
        [SerializeField] private List<Minimap> maps = new List<Minimap>();
        [SerializeField] private Vector2 worldSize;
        [SerializeField] private Vector2 fullScreenDimensions = new Vector2(1000, 1000);
        [SerializeField] private float zoomSpeed = 0.1f;
        [SerializeField] private float maxZoom = 10f;
        [SerializeField] private float minZoom = 1f;
        [SerializeField, Required] private MinimapIcon minimapIconPrefab;


        public IReadOnlyList<Minimap> Maps => maps;
        public Vector2 WorldSize => worldSize;
        public Vector2 FullScreenDimensions => fullScreenDimensions;
        public float ZoomSpeed => zoomSpeed;
        public float MaxZoom => maxZoom;
        public float MinZoom => minZoom;
        public MinimapIcon MinimapIconPrefab => minimapIconPrefab;
    }
}
