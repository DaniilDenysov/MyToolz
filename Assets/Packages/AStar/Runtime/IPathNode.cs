using System.Collections.Generic;

namespace MyToolz.Algorithms.AStar
{
    public interface IPathNode<TPos>
    {
        TPos Position { get; }
        bool Walkable { get; }
        float TraversalCost { get; }
    }

    public interface INeighborProvider<TPos>
    {
        IEnumerable<TPos> GetNeighbors(TPos position);
    }

    public interface IHeuristic<TPos>
    {
        float Estimate(TPos from, TPos to);
    }

    public interface INodeLookup<TPos, TNode> where TNode : IPathNode<TPos>
    {
        bool TryGet(TPos position, out TNode node);
    }
}
