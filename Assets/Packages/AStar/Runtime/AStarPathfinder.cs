using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyToolz.Algorithms.AStar
{
    public sealed class AStarPathfinder<TPos, TNode>
        where TNode : IPathNode<TPos>
    {
        private readonly INodeLookup<TPos, TNode> _lookup;
        private readonly INeighborProvider<TPos> _neighborProvider;
        private readonly IHeuristic<TPos> _heuristic;
        private readonly IEqualityComparer<TPos> _comparer;

        public AStarPathfinder(
            INodeLookup<TPos, TNode> lookup,
            INeighborProvider<TPos> neighborProvider,
            IHeuristic<TPos> heuristic,
            IEqualityComparer<TPos> comparer = null)
        {
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            _neighborProvider = neighborProvider ?? throw new ArgumentNullException(nameof(neighborProvider));
            _heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
            _comparer = comparer ?? EqualityComparer<TPos>.Default;
        }

        public Task<PathResult<TPos>> FindPathAsync(
            TPos start,
            TPos goal,
            CancellationToken ct = default)
        {
            return Task.Run(() => FindPath(start, goal, ct), ct);
        }

        public PathResult<TPos> FindPath(
            TPos start,
            TPos goal,
            CancellationToken ct = default)
        {
            if (!_lookup.TryGet(start, out var startNode) || !startNode.Walkable)
                return PathResult<TPos>.Failed;

            if (!_lookup.TryGet(goal, out var goalNode) || !goalNode.Walkable)
                return PathResult<TPos>.Failed;

            if (_comparer.Equals(start, goal))
                return new PathResult<TPos>(true, new[] { start }, 0f);

            var gScores = new Dictionary<TPos, float>(_comparer);
            var parents = new Dictionary<TPos, TPos>(_comparer);
            var closed = new HashSet<TPos>(_comparer);
            var open = new BinaryMinHeap<TPos>(128);

            gScores[start] = 0f;
            open.Enqueue(start, _heuristic.Estimate(start, goal));

            while (open.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                TPos current = open.Dequeue();

                if (closed.Contains(current))
                    continue;

                if (_comparer.Equals(current, goal))
                    return ReconstructPath(parents, gScores, current);

                closed.Add(current);

                float currentG = gScores[current];

                foreach (TPos neighborPos in _neighborProvider.GetNeighbors(current))
                {
                    if (closed.Contains(neighborPos))
                        continue;

                    if (!_lookup.TryGet(neighborPos, out var neighborNode))
                        continue;

                    if (!neighborNode.Walkable)
                        continue;

                    float cost = neighborNode.TraversalCost;
                    if (cost < 0f) cost = 0f;

                    float tentativeG = currentG + cost;

                    if (gScores.TryGetValue(neighborPos, out float existingG) && tentativeG >= existingG)
                        continue;

                    gScores[neighborPos] = tentativeG;
                    parents[neighborPos] = current;

                    float f = tentativeG + _heuristic.Estimate(neighborPos, goal);
                    open.Enqueue(neighborPos, f);
                }
            }

            return PathResult<TPos>.Failed;
        }

        private PathResult<TPos> ReconstructPath(
            Dictionary<TPos, TPos> parents,
            Dictionary<TPos, float> gScores,
            TPos goal)
        {
            var path = new List<TPos>();
            TPos current = goal;

            while (true)
            {
                path.Add(current);
                if (!parents.TryGetValue(current, out TPos parent))
                    break;
                current = parent;
            }

            path.Reverse();
            gScores.TryGetValue(goal, out float totalCost);
            return new PathResult<TPos>(true, path, totalCost);
        }
    }
}
