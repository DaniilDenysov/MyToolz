using System;
using System.Collections.Generic;
using MyToolz.Algorithms.AStar;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class AStarTests
    {
        private readonly struct Cell : IPathNode<(int x, int y)>
        {
            public Cell((int x, int y) position, bool walkable, float cost)
            {
                Position = position;
                Walkable = walkable;
                TraversalCost = cost;
            }

            public (int x, int y) Position { get; }
            public bool Walkable { get; }
            public float TraversalCost { get; }
        }

        /// <summary>Grid graph used by every test; also the heuristic, lookup and neighbour source.</summary>
        private sealed class Grid : INodeLookup<(int x, int y), Cell>, INeighborProvider<(int x, int y)>, IHeuristic<(int x, int y)>
        {
            private readonly Cell[,] cells;
            public Func<(int x, int y), (int x, int y), float> HeuristicOverride;

            public Grid(int width, int height, Func<int, int, float> cost)
            {
                cells = new Cell[width, height];
                for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    float c = cost(x, y);
                    cells[x, y] = new Cell((x, y), true, c);
                }
            }

            public void Block(int x, int y) => cells[x, y] = new Cell((x, y), false, 1f);

            public bool TryGet((int x, int y) p, out Cell node)
            {
                node = default;
                if (p.x < 0 || p.y < 0 || p.x >= cells.GetLength(0) || p.y >= cells.GetLength(1)) return false;
                node = cells[p.x, p.y];
                return true;
            }

            public IEnumerable<(int x, int y)> GetNeighbors((int x, int y) p)
            {
                yield return (p.x + 1, p.y);
                yield return (p.x - 1, p.y);
                yield return (p.x, p.y + 1);
                yield return (p.x, p.y - 1);
            }

            public float Estimate((int x, int y) from, (int x, int y) to) =>
                HeuristicOverride?.Invoke(from, to) ?? Math.Abs(from.x - to.x) + Math.Abs(from.y - to.y);
        }

        private sealed class ZeroHeuristic : IHeuristic<(int x, int y)>
        {
            public float Estimate((int x, int y) from, (int x, int y) to) => 0f;
        }

        private static AStarPathfinder<(int x, int y), Cell> Finder(Grid grid, IHeuristic<(int x, int y)> heuristic = null) =>
            new AStarPathfinder<(int x, int y), Cell>(grid, grid, heuristic ?? grid);

        [Test]
        public void FindsStraightPath_WithUnitCosts()
        {
            var grid = new Grid(5, 1, (x, y) => 1f);

            var result = Finder(grid).FindPath((0, 0), (4, 0));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, result.Positions.Count);
            Assert.AreEqual(4f, result.TotalCost);
        }

        [Test]
        public void RoutesAroundWalls()
        {
            var grid = new Grid(3, 3, (x, y) => 1f);
            grid.Block(1, 0);
            grid.Block(1, 1);

            var result = Finder(grid).FindPath((0, 0), (2, 0));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(6f, result.TotalCost);
        }

        [Test]
        public void UnreachableGoal_Fails()
        {
            var grid = new Grid(3, 1, (x, y) => 1f);
            grid.Block(1, 0);

            Assert.IsFalse(Finder(grid).FindPath((0, 0), (2, 0)).Success);
        }

        [Test]
        public void NaNAndInfiniteCosts_AreImpassable()
        {
            var grid = new Grid(3, 2, (x, y) => x == 1 && y == 0 ? float.NaN : (x == 1 && y == 1 ? float.PositiveInfinity : 1f));

            Assert.IsFalse(Finder(grid).FindPath((0, 0), (2, 0)).Success);
        }

        [Test]
        public void MatchesDijkstra_OnWeightedGrids()
        {
            var random = new Random(1234);
            for (int trial = 0; trial < 25; trial++)
            {
                var grid = new Grid(8, 8, (x, y) => 1 + random.Next(0, 9));
                for (int i = 0; i < 10; i++) grid.Block(random.Next(1, 7), random.Next(1, 7));

                var astar = Finder(grid).FindPath((0, 0), (7, 7));
                var dijkstra = Finder(grid, new ZeroHeuristic()).FindPath((0, 0), (7, 7));

                Assert.AreEqual(dijkstra.Success, astar.Success, $"trial {trial}");
                if (astar.Success) Assert.AreEqual(dijkstra.TotalCost, astar.TotalCost, 1e-4, $"trial {trial}");
            }
        }

        [Test]
        public void InconsistentButAdmissibleHeuristic_StillFindsTheOptimalPath()
        {
            // Layout (goal G at (3,1), # blocked):
            //   y=1:  P  X  .  G
            //   y=0:  S  b  #  #      entering b costs 2, every other cell 1.
            // h(P) = 3 is admissible (true remaining cost from P is 3) but not consistent. X is first
            // expanded through b with cost 3; the cheaper route through P (cost 2) only appears after X
            // is closed. Without reopening X the search returns 5 instead of the optimal 4.
            var grid = new Grid(4, 2, (x, y) => x == 1 && y == 0 ? 2f : 1f);
            grid.Block(2, 0);
            grid.Block(3, 0);
            grid.HeuristicOverride = (from, to) => from == (0, 1) ? 3f : 0f;

            var astar = Finder(grid).FindPath((0, 0), (3, 1));
            var dijkstra = Finder(grid, new ZeroHeuristic()).FindPath((0, 0), (3, 1));

            Assert.AreEqual(4f, dijkstra.TotalCost, 1e-4);
            Assert.AreEqual(dijkstra.TotalCost, astar.TotalCost, 1e-4);
        }

        [Test]
        public void ExpansionLimit_StopsTheSearch()
        {
            var grid = new Grid(50, 50, (x, y) => 1f);
            var finder = Finder(grid, new ZeroHeuristic());
            finder.MaxExpandedNodes = 10;

            Assert.IsFalse(finder.FindPath((0, 0), (49, 49)).Success);
        }

        [Test]
        public void Cancellation_Throws()
        {
            var grid = new Grid(10, 10, (x, y) => 1f);
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => Finder(grid).FindPath((0, 0), (9, 9), cts.Token));
        }
    }
}
