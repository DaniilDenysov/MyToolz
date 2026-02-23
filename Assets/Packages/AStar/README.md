# A* Pathfinder — Abstract & Optimized

A generic, thread-safe A\* pathfinding library with zero engine dependencies.
The algorithm is provided ready to use; all domain-specific logic (grid shape, coordinate system, heuristic, neighbor rules) is left for the consumer to implement via clean interfaces.

---

## Architecture

```
Core/
├── IPathNode.cs           Interfaces: IPathNode, INodeLookup, INeighborProvider, IHeuristic
├── PathResult.cs          Immutable result struct returned by the pathfinder
├── BinaryMinHeap.cs       Array-backed min-heap used as the open set
└── AStarPathfinder.cs     The algorithm — generic over position type and node type

Examples/
└── SquareGridExample.cs   Complete working example on a 10×10 grid with walls
```

### Key interfaces

| Interface | Purpose |
|---|---|
| `IPathNode<TPos>` | Represents a single map node. Exposes `Position`, `Walkable`, `TraversalCost`. |
| `INodeLookup<TPos, TNode>` | Resolves a position to its node. Replaces the old `IMap` and decouples storage from algorithm. |
| `INeighborProvider<TPos>` | Computes neighbors on demand for a given position. Eliminates per-tile neighbor storage. |
| `IHeuristic<TPos>` | Estimates remaining cost between two positions. |

### Flow

```
User code                          Library
────────                           ───────
Implement interfaces ──────────►  AStarPathfinder<TPos, TNode>
                                       │
Call FindPathAsync(start, goal) ──►   Task.Run ──► FindPath (runs on thread pool)
                                       │
await result  ◄────────────────── PathResult<TPos>
```

---

## Usage

### 1. Define your position type

Any type works (`Vector2Int`, `(int,int)`, a custom struct, etc.).
For dictionary performance, implement `IEquatable<T>` and override `GetHashCode`.

```csharp
public struct GridPosition : IEquatable<GridPosition>
{
    public readonly int X, Y;
    public GridPosition(int x, int y) { X = x; Y = y; }
    public bool Equals(GridPosition o) => X == o.X && Y == o.Y;
    public override int GetHashCode() => X * 397 ^ Y;
}
```

### 2. Implement `IPathNode<TPos>`

```csharp
public sealed class GridNode : IPathNode<GridPosition>
{
    public GridPosition Position { get; }
    public bool Walkable { get; }
    public float TraversalCost { get; }

    public GridNode(GridPosition pos, bool walkable, float cost = 1f)
    {
        Position = pos;
        Walkable = walkable;
        TraversalCost = cost;
    }
}
```

### 3. Implement `INodeLookup`

Wraps whatever data structure holds your map:

```csharp
public sealed class GridLookup : INodeLookup<GridPosition, GridNode>
{
    private readonly GridNode[,] _grid;
    public GridLookup(GridNode[,] grid) => _grid = grid;

    public bool TryGet(GridPosition pos, out GridNode node)
    {
        if ((uint)pos.X < (uint)_grid.GetLength(0) &&
            (uint)pos.Y < (uint)_grid.GetLength(1))
        {
            node = _grid[pos.X, pos.Y];
            return node != null;
        }
        node = null;
        return false;
    }
}
```

### 4. Implement `INeighborProvider`

Neighbors are computed on demand — nothing stored per tile:

```csharp
public sealed class FourWayNeighbors : INeighborProvider<GridPosition>
{
    public IEnumerable<GridPosition> GetNeighbors(GridPosition p)
    {
        yield return new GridPosition(p.X, p.Y + 1);
        yield return new GridPosition(p.X, p.Y - 1);
        yield return new GridPosition(p.X - 1, p.Y);
        yield return new GridPosition(p.X + 1, p.Y);
    }
}
```

### 5. Implement `IHeuristic`

```csharp
public sealed class ManhattanHeuristic : IHeuristic<GridPosition>
{
    public float Estimate(GridPosition a, GridPosition b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
```

### 6. Run

```csharp
var pathfinder = new AStarPathfinder<GridPosition, GridNode>(
    lookup, neighbors, heuristic);

// Async (recommended) — runs on thread pool, never blocks main thread
PathResult<GridPosition> result = await pathfinder.FindPathAsync(start, goal, ct);

// Synchronous alternative
PathResult<GridPosition> result = pathfinder.FindPath(start, goal);

if (result.Success)
    foreach (var pos in result.Positions)
        Console.WriteLine(pos);
```

---

## Changes from the original

### 1. Open set → binary min-heap
The sorted-list open set was replaced with `BinaryMinHeap<T>`, an array-backed binary heap with separate priority storage. Insert and extract-min are O(log n) without any per-iteration sort.

### 2. Neighbor computation on demand
`INeighborProvider<TPos>` replaces storing neighbor lists on every tile.
Neighbors are yielded lazily per expansion, which eliminates per-node memory overhead entirely. Consumers provide the neighborhood shape (4-way, 8-way, hex, 3D, etc.) as a standalone class.

### 3. `async void` → `async Task` with cancellation
`FindPathAsync` returns `Task<PathResult<TPos>>` — exceptions propagate normally to the caller's `await`. A `CancellationToken` is checked every iteration so callers can enforce timeouts or abort long searches.

### 4. Engine decoupling
All Unity types (`Vector3`, `MonoBehaviour`, `ScriptableObject`) are removed from the core. The algorithm operates on abstract `TPos`/`TNode` — usable in Unity, Godot, MonoGame, server-side, or plain console apps without any engine reference.

### 5. Removed game-specific types
`CubicVector3`, `MapSO`, `TerrainTileSO`, `SerializedDictionary`, `PositionToMapTileDictionary`, `MapTile2D`, `MapTile3D`, `NDArrayMap`, `NDArrayTileBase`, and the built-in heuristics tied to Unity vectors are all removed from core. The example shows how to reimplement equivalent functionality in a few lines.

### 6. Separation of concerns
The original `Path.cs` bundled interfaces, data structures, algorithm, and multiple concrete implementations in one file. These are now split into focused files with a clear core/example boundary.
