using System.Collections.Generic;

namespace MyToolz.Algorithms.AStar
{
    public readonly struct PathResult<TPos>
    {
        public bool Success { get; }
        public IReadOnlyList<TPos> Positions { get; }
        public float TotalCost { get; }

        public static PathResult<TPos> Failed => new PathResult<TPos>(false, null, 0f);

        public PathResult(bool success, IReadOnlyList<TPos> positions, float totalCost)
        {
            Success = success;
            Positions = positions;
            TotalCost = totalCost;
        }
    }
}
