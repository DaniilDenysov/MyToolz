using UnityEngine;

namespace MyToolz.Algorithms.AStar.Demo
{
    public class Tile : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _baseColor;

        public GridPosition GridPosition { get; private set; }
        public bool Walkable { get; private set; }

        public void Init(GridPosition position, bool walkable)
        {
            GridPosition = position;
            Walkable = walkable;
            _renderer = GetComponent<Renderer>();
            _baseColor = walkable ? Color.white : Color.black;
            _renderer.material.color = _baseColor;
        }

        public void SetColor(Color color) => _renderer.material.color = color;
        public void ResetColor() => _renderer.material.color = _baseColor;
    }
}