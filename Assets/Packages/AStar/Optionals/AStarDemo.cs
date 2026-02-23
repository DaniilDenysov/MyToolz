using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MyToolz.Algorithms.AStar;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyToolz.Algorithms.AStar.Demo
{
    public struct GridPosition : System.IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y) { X = x; Y = y; }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition gp && Equals(gp);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X}, {Y})";
    }

    public class GridNode : IPathNode<GridPosition>
    {
        public GridPosition Position { get; }
        public bool Walkable { get; set; }
        public float TraversalCost { get; }

        public GridNode(GridPosition position, bool walkable, float traversalCost = 1f)
        {
            Position = position;
            Walkable = walkable;
            TraversalCost = traversalCost;
        }
    }

    public class GridLookup : INodeLookup<GridPosition, GridNode>
    {
        private readonly GridNode[,] _grid;
        private readonly int _width;
        private readonly int _height;

        public GridLookup(GridNode[,] grid)
        {
            _grid = grid;
            _width = grid.GetLength(0);
            _height = grid.GetLength(1);
        }

        public bool TryGet(GridPosition position, out GridNode node)
        {
            if ((uint)position.X < (uint)_width && (uint)position.Y < (uint)_height)
            {
                node = _grid[position.X, position.Y];
                return node != null;
            }
            node = null;
            return false;
        }
    }

    public class FourDirectionNeighborProvider : INeighborProvider<GridPosition>
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(0, -1),
            new GridPosition(-1, 0),
            new GridPosition(1, 0)
        };

        public IEnumerable<GridPosition> GetNeighbors(GridPosition position)
        {
            for (int i = 0; i < Offsets.Length; i++)
            {
                yield return new GridPosition(
                    position.X + Offsets[i].X,
                    position.Y + Offsets[i].Y);
            }
        }
    }

    public class ManhattanHeuristic : IHeuristic<GridPosition>
    {
        public float Estimate(GridPosition from, GridPosition to)
        {
            return Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
        }
    }


    [RequireComponent(typeof(LineRenderer))]
    public class AStarDemo : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Transform gridPivot;
        [SerializeField] private int width = 15;
        [SerializeField] private int height = 15;
        [SerializeField] [Range(0f, 0.4f)] private float wallDensity = 0.25f;
        [SerializeField] private float tileSpacing = 1.1f;

        [Header("Path")]
        [SerializeField] private float pathHeight = 0.5f;
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color startColor = Color.green;
        [SerializeField] private Color goalColor = Color.red;

        [Header("Timing")]
        [SerializeField] private float newPathInterval = 2f;
        [SerializeField] private TMP_Text pathTimeText;

        private GridNode[,] grid;
        private Tile[,] tiles;
        private List<Tile> walkableTiles;
        private AStarPathfinder<GridPosition, GridNode> pathfinder;
        private LineRenderer lineRenderer;
        private CancellationTokenSource cts;
        private Tile currentStart;
        private Tile currentGoal;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = pathColor;
            lineRenderer.endColor = pathColor;
            lineRenderer.positionCount = 0;
        }

        private void Start()
        {
            BuildGrid();

            var lookup = new GridLookup(grid);
            var neighbors = new FourDirectionNeighborProvider();
            var heuristic = new ManhattanHeuristic();
            pathfinder = new AStarPathfinder<GridPosition, GridNode>(lookup, neighbors, heuristic);

            InvokeRepeating(nameof(RequestRandomPath), 0.5f, newPathInterval);
        }

        private void OnDestroy()
        {
            CancelPending();
        }

        private void BuildGrid()
        {
            grid = new GridNode[width, height];
            tiles = new Tile[width, height];
            walkableTiles = new List<Tile>(width * height);

            Vector3 offset = new Vector3(
                -(width - 1) * tileSpacing * 0.5f,
                0f,
                -(height - 1) * tileSpacing * 0.5f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool walkable = Random.value > wallDensity;
                    var pos = new GridPosition(x, y);

                    grid[x, y] = new GridNode(pos, walkable);

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"Tile_{x}_{y}";
                    go.transform.SetParent(gridPivot);
                    go.transform.localPosition = offset + new Vector3(
                        x * tileSpacing, y * tileSpacing, 0f);

                    var tile = go.AddComponent<Tile>();
                    tile.Init(pos, walkable);
                    tiles[x, y] = tile;

                    if (walkable)
                        walkableTiles.Add(tile);
                }
            }
        }

        private void RequestRandomPath()
        {
            if (walkableTiles.Count < 2)
                return;

            Tile startTile = walkableTiles[Random.Range(0, walkableTiles.Count)];
            Tile goalTile;
            do { goalTile = walkableTiles[Random.Range(0, walkableTiles.Count)]; }
            while (goalTile == startTile);

            FindAndShowPath(startTile, goalTile);
        }

        private async void FindAndShowPath(Tile startTile, Tile goalTile)
        {
            CancelPending();
            ClearVisualization();

            currentStart = startTile;
            currentGoal = goalTile;
            startTile.SetColor(startColor);
            goalTile.SetColor(goalColor);

            cts = new CancellationTokenSource(System.TimeSpan.FromSeconds(5));
            var token = cts.Token;

            try
            {
                var sw = Stopwatch.StartNew();
                PathResult<GridPosition> result = await pathfinder.FindPathAsync(
                    startTile.GridPosition, goalTile.GridPosition, token);
                sw.Stop();

                if (token.IsCancellationRequested || this == null)
                    return;

                if (pathTimeText != null)
                    pathTimeText.text = $"{sw.Elapsed.TotalMilliseconds:F3} ms";

                if (result.Success)
                    DrawPath(result.Positions);
                else
                    Debug.Log($"No path from {startTile.GridPosition} to {goalTile.GridPosition}");
            }
            catch (TaskCanceledException) { }
            catch (System.Exception ex)
            {
                Debug.LogError($"Pathfinding error: {ex.Message}");
            }
        }

        private void DrawPath(IReadOnlyList<GridPosition> positions)
        {
            lineRenderer.positionCount = positions.Count;

            for (int i = 0; i < positions.Count; i++)
            {
                var gp = positions[i];
                Vector3 world = tiles[gp.X, gp.Y].transform.position;
                world.z += pathHeight;
                lineRenderer.SetPosition(i, world);
            }
        }

        private void ClearVisualization()
        {
            lineRenderer.positionCount = 0;
            if (currentStart != null) currentStart.ResetColor();
            if (currentGoal != null) currentGoal.ResetColor();
        }

        private void CancelPending()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }
}
