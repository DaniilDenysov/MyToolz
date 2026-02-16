using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyToolz.Core;

namespace MyToolz.Pathfinding
{
    public interface IMap<TPos, TTile> where TTile : IMapTile<TPos>
    {
        bool TryGetTile(TPos pos, out TTile tile);
    }

    public interface IMapTile<TPos>
    {
        TPos Position { get; }
        Vector3 WorldPosition { get; }
        bool Walkable { get; }
        int TerrainCost { get; }
        IEnumerable<TPos> GetNeighbourPositions();
    }

    public interface IHeuristic<TPos>
    {
        float Estimate(TPos from, TPos to);
    }

    public class EuclideanWorldHeuristic<TPos, TTile> : IHeuristic<TPos> where TTile : IMapTile<TPos>
    {
        private readonly Func<TPos, TTile> tileResolver;
        public EuclideanWorldHeuristic(Func<TPos, TTile> tileResolver) => this.tileResolver = tileResolver;
        public float Estimate(TPos from, TPos to) => Vector3.Distance(tileResolver(from).WorldPosition, tileResolver(to).WorldPosition);
    }

    public class IntArrayComparer : IEqualityComparer<int[]>
    {
        public static readonly IntArrayComparer Instance = new IntArrayComparer();
        public bool Equals(int[] a, int[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public int GetHashCode(int[] a)
        {
            if (a == null) return 0;
            unchecked
            {
                int h = 17;
                for (int i = 0; i < a.Length; i++)
                    h = h * 31 + a[i];
                return h;
            }
        }
    }

    public class NDArrayMap<TTile> : IMap<int[], TTile> where TTile : class, IMapTile<int[]>
    {
        private readonly Array tiles;
        private readonly int rank;
        private readonly int[] lengths;

        public NDArrayMap(Array tiles)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            if (tiles.Rank <= 0) throw new ArgumentException("Array must have Rank >= 1.", nameof(tiles));
            rank = tiles.Rank;
            lengths = new int[rank];
            for (int i = 0; i < rank; i++) lengths[i] = tiles.GetLength(i);
        }

        public bool TryGetTile(int[] pos, out TTile tile)
        {
            tile = null;
            if (pos == null || pos.Length != rank) return false;
            for (int i = 0; i < rank; i++)
                if ((uint)pos[i] >= (uint)lengths[i])
                    return false;

            tile = (TTile)tiles.GetValue(pos);
            return tile != null;
        }
    }

    public abstract class NDArrayTileBase : IMapTile<int[]>
    {
        private readonly int[] position;
        private readonly Vector3 worldPosition;
        private readonly bool walkable;
        private readonly int terrainCost;
        private readonly int[][] neighbourOffsets;

        public int[] Position => position;
        public Vector3 WorldPosition => worldPosition;
        public bool Walkable => walkable;
        public int TerrainCost => terrainCost;

        protected NDArrayTileBase(int[] position, Vector3 worldPosition, bool walkable, int terrainCost, int[][] neighbourOffsets)
        {
            this.position = position;
            this.worldPosition = worldPosition;
            this.walkable = walkable;
            this.terrainCost = terrainCost;
            this.neighbourOffsets = neighbourOffsets ?? throw new ArgumentNullException(nameof(neighbourOffsets));
        }

        public IEnumerable<int[]> GetNeighbourPositions()
        {
            for (int i = 0; i < neighbourOffsets.Length; i++)
            {
                var o = neighbourOffsets[i];
                var n = new int[position.Length];
                for (int d = 0; d < n.Length; d++)
                    n[d] = position[d] + o[d];
                yield return n;
            }
        }
    }

    public static class NeighbourOffsets
    {
        public static int[][] VonNeumann(int dimensions)
        {
            if (dimensions <= 0) throw new ArgumentOutOfRangeException(nameof(dimensions));
            var list = new List<int[]>(dimensions * 2);
            for (int d = 0; d < dimensions; d++)
            {
                var plus = new int[dimensions];
                var minus = new int[dimensions];
                plus[d] = 1;
                minus[d] = -1;
                list.Add(plus);
                list.Add(minus);
            }
            return list.ToArray();
        }
    }

    public class BinaryMinHeap<T>
    {
        private struct Node
        {
            public T Item;
            public float Priority;
            public Node(T item, float priority) { Item = item; Priority = priority; }
        }

        private Node[] data;
        private int count;

        public int Count => count;

        public BinaryMinHeap(int capacity = 128)
        {
            if (capacity < 4) capacity = 4;
            data = new Node[capacity];
            count = 0;
        }

        public void Push(T item, float priority)
        {
            if (count == data.Length) Array.Resize(ref data, data.Length * 2);
            data[count] = new Node(item, priority);
            SiftUp(count++);
        }

        public T Pop(out float priority)
        {
            var root = data[0];
            priority = root.Priority;
            count--;
            if (count > 0)
            {
                data[0] = data[count];
                SiftDown(0);
            }
            return root.Item;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (data[i].Priority >= data[p].Priority) break;
                (data[i], data[p]) = (data[p], data[i]);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int l = (i << 1) + 1;
                if (l >= count) break;
                int r = l + 1;
                int m = (r < count && data[r].Priority < data[l].Priority) ? r : l;
                if (data[i].Priority <= data[m].Priority) break;
                (data[i], data[m]) = (data[m], data[i]);
                i = m;
            }
        }
    }

    public class AStarPath<TPos, TTile> : ObjectPlus where TTile : class, IMapTile<TPos>
    {
        private readonly IMap<TPos, TTile> map;
        private readonly IHeuristic<TPos> heuristic;
        private readonly IEqualityComparer<TPos> posComparer;

        private class Node
        {
            public TPos Pos;
            public Node Parent;
            public float G;
            public float H;
            public float F => G + H;
            public Node(TPos pos) { Pos = pos; G = float.PositiveInfinity; }
        }

        public AStarPath(IMap<TPos, TTile> map, IHeuristic<TPos> heuristic, IEqualityComparer<TPos> posComparer = null)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
            this.posComparer = posComparer ?? EqualityComparer<TPos>.Default;
        }

        public Task<List<Vector3>> FindAsync(TTile startTile, TTile endTile, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Find(startTile, endTile), cancellationToken);
        }

        public List<Vector3> Find(TTile startTile, TTile endTile)
        {
            if (startTile == null || endTile == null) return new List<Vector3>();
            if (!startTile.Walkable || !endTile.Walkable) return new List<Vector3>();

            var nodes = new Dictionary<TPos, Node>(posComparer);
            var closed = new HashSet<TPos>(posComparer);
            var open = new BinaryMinHeap<Node>();

            Node GetNode(TPos p)
            {
                if (!nodes.TryGetValue(p, out var n))
                {
                    n = new Node(p);
                    nodes[p] = n;
                }
                return n;
            }

            var start = GetNode(startTile.Position);
            start.G = 0f;
            start.H = heuristic.Estimate(startTile.Position, endTile.Position);
            open.Push(start, start.F);

            while (open.Count > 0)
            {
                var current = open.Pop(out _);
                if (closed.Contains(current.Pos)) continue;

                if (posComparer.Equals(current.Pos, endTile.Position))
                    return SimplifyPath(current);

                closed.Add(current.Pos);

                if (!map.TryGetTile(current.Pos, out var currentTile) || currentTile == null) continue;

                foreach (var npos in currentTile.GetNeighbourPositions())
                {
                    if (closed.Contains(npos)) continue;
                    if (!map.TryGetTile(npos, out var ntile) || ntile == null) continue;
                    if (!ntile.Walkable) continue;

                    var neighbor = GetNode(npos);

                    float tentativeG = current.G + Mathf.Max(0, ntile.TerrainCost);
                    if (tentativeG >= neighbor.G) continue;

                    neighbor.Parent = current;
                    neighbor.G = tentativeG;
                    neighbor.H = heuristic.Estimate(npos, endTile.Position);

                    open.Push(neighbor, neighbor.F);
                }
            }

            return new List<Vector3>();
        }

        private List<Vector3> SimplifyPath(Node endNode)
        {
            var points = new List<Vector3>();
            Node prev = null;
            Node cur = endNode;
            Vector3? lastDir = null;

            while (cur != null)
            {
                if (!map.TryGetTile(cur.Pos, out var curTile) || curTile == null) break;

                if (prev != null)
                {
                    if (!map.TryGetTile(prev.Pos, out var prevTile) || prevTile == null) break;

                    var dir = (prevTile.WorldPosition - curTile.WorldPosition).normalized;
                    if (lastDir == null || Vector3.Angle(dir, lastDir.Value) > 1e-2f)
                    {
                        points.Add(prevTile.WorldPosition);
                        lastDir = dir;
                    }
                }

                prev = cur;
                cur = cur.Parent;
            }

            if (prev != null && map.TryGetTile(prev.Pos, out var lastTile) && lastTile != null)
                points.Add(lastTile.WorldPosition);

            points.Reverse();
            return points;
        }
    }

    public sealed class GridWorldHeuristic2D : IHeuristic<Vector2Int>
    {
        public float Estimate(Vector2Int from, Vector2Int to) => Vector2Int.Distance(from, to);
    }

    public sealed class GridWorldHeuristic3D : IHeuristic<Vector3Int>
    {
        public float Estimate(Vector3Int from, Vector3Int to) => Vector3Int.Distance(from, to);
    }

    public abstract class MapTile2D : IMapTile<Vector2Int>
    {
        public Vector2Int Position { get; protected set; }
        public Vector3 WorldPosition { get; protected set; }
        public bool Walkable { get; protected set; } = true;
        public int TerrainCost { get; protected set; } = 1;

        public virtual IEnumerable<Vector2Int> GetNeighbourPositions()
        {
            yield return Position + Vector2Int.up;
            yield return Position + Vector2Int.down;
            yield return Position + Vector2Int.left;
            yield return Position + Vector2Int.right;
        }
    }

    public abstract class MapTile3D : IMapTile<Vector3Int>
    {
        public Vector3Int Position { get; protected set; }
        public Vector3 WorldPosition { get; protected set; }
        public bool Walkable { get; protected set; } = true;
        public int TerrainCost { get; protected set; } = 1;

        public virtual IEnumerable<Vector3Int> GetNeighbourPositions()
        {
            yield return Position + Vector3Int.up;
            yield return Position + Vector3Int.down;
            yield return Position + Vector3Int.left;
            yield return Position + Vector3Int.right;
            yield return Position + new Vector3Int(0, 0, 1);
            yield return Position + new Vector3Int(0, 0, -1);
        }
    }
}
