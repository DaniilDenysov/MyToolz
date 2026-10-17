using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyToolz.Algorithms.AStar
{
    /// <summary>
    /// A* search over any graph described by a node lookup, a neighbour provider and a heuristic.
    ///
    /// Contract:
    /// - The path is optimal when the heuristic never overestimates the remaining cost (admissible).
    ///   Nodes are reopened when a cheaper route is found, so a merely admissible (not consistent)
    ///   heuristic still yields the optimal path. An overestimating heuristic trades optimality for speed.
    /// - Moving onto a node costs its <see cref="IPathNode{TPos}.TraversalCost"/>. Negative costs count
    ///   as 0; NaN or infinite costs make the node impassable. NaN/negative estimates count as 0.
    /// - <see cref="FindPathAsync"/> runs on a worker thread: the lookup, neighbour provider and
    ///   heuristic must then be safe to read from that thread (use an immutable snapshot of the graph,
    ///   not live scene objects). WebGL has no worker threads; call <see cref="FindPath"/> there.
    /// </summary>
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

        /// <summary>Stops the search after this many node expansions (guards unbounded graphs). Default: no limit.</summary>
        public int MaxExpandedNodes { get; set; } = int.MaxValue;

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
            int expanded = 0;

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
            open.Enqueue(start, Estimate(start, goal));

            while (open.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                TPos current = open.Dequeue();

                if (closed.Contains(current))
                    continue;

                if (_comparer.Equals(current, goal))
                    return ReconstructPath(parents, gScores, current);

                closed.Add(current);
                if (++expanded > MaxExpandedNodes)
                    return PathResult<TPos>.Failed;

                float currentG = gScores[current];

                var neighbors = _neighborProvider.GetNeighbors(current);
                if (neighbors == null)
                    continue;

                foreach (TPos neighborPos in neighbors)
                {
                    if (!_lookup.TryGet(neighborPos, out var neighborNode))
                        continue;

                    if (!neighborNode.Walkable)
                        continue;

                    float cost = neighborNode.TraversalCost;
                    if (float.IsNaN(cost) || float.IsInfinity(cost))
                        continue;
                    if (cost < 0f) cost = 0f;

                    float tentativeG = currentG + cost;

                    if (gScores.TryGetValue(neighborPos, out float existingG) && tentativeG >= existingG)
                        continue;

                    // A cheaper route to an already expanded node reopens it (needed for optimality
                    // with admissible but inconsistent heuristics).
                    closed.Remove(neighborPos);

                    gScores[neighborPos] = tentativeG;
                    parents[neighborPos] = current;

                    float f = tentativeG + Estimate(neighborPos, goal);
                    open.Enqueue(neighborPos, f);
                }
            }

            return PathResult<TPos>.Failed;
        }

        private float Estimate(TPos from, TPos to)
        {
            float estimate = _heuristic.Estimate(from, to);
            return float.IsNaN(estimate) || estimate < 0f ? 0f : estimate;
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
