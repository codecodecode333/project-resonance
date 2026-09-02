using System;
using System.Collections.Generic;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using ProjectResonance.Units;

namespace ProjectResonance.Movement
{
    public sealed class PathFinder
    {
        private readonly GridState _gridState;
        private readonly GridTraversal _gridTraversal;

        public PathFinder(GridState gridState, GridTraversal gridTraversal)
        {
            _gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            _gridTraversal = gridTraversal ?? throw new ArgumentNullException(nameof(gridTraversal));
        }

        /// <summary>
        /// Finds one shortest unit-cost path, excluding the start and including the target.
        /// Start equal to target succeeds with an empty path; no route returns false with an empty path.
        /// Equal-length route selection is unspecified. This query does not change game state.
        /// </summary>
        public bool TryFindPath(
            UnitState mover,
            GridPosition target,
            int maxDistance,
            out IReadOnlyList<GridPosition> path)
        {
            path = Array.Empty<GridPosition>();

            if (mover == null)
            {
                throw new ArgumentNullException(nameof(mover));
            }

            if (maxDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDistance), maxDistance, "Maximum distance cannot be negative.");
            }

            if (!mover.IsPlaced || !_gridState.IsInBounds(mover.Position))
            {
                throw new InvalidOperationException("Mover must be placed within this grid.");
            }

            var start = mover.Position;
            if (start == target)
            {
                // A zero-step path does not require the already occupied start to be stoppable.
                return true;
            }

            if (maxDistance == 0 || !_gridTraversal.CanStopAt(mover, target))
            {
                return false;
            }

            var queue = new Queue<GridPosition>();
            var distances = new Dictionary<GridPosition, int> { { start, 0 } };
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distances[current];
                if (currentDistance >= maxDistance)
                {
                    continue;
                }

                var nextDistance = currentDistance + 1;
                foreach (var neighbor in _gridState.GetOrthogonalNeighbors(current))
                {
                    var position = neighbor.Position;
                    if (distances.ContainsKey(position)
                        || !_gridTraversal.CanPassThrough(mover, current, position))
                    {
                        continue;
                    }

                    // First discovery gives both the shortest distance and an acyclic predecessor.
                    distances.Add(position, nextDistance);
                    cameFrom.Add(position, current);
                    if (position == target)
                    {
                        path = ReconstructPath(start, target, cameFrom);
                        return true;
                    }

                    queue.Enqueue(position);
                }
            }

            return false;
        }

        private static IReadOnlyList<GridPosition> ReconstructPath(
            GridPosition start,
            GridPosition target,
            Dictionary<GridPosition, GridPosition> cameFrom)
        {
            var steps = new List<GridPosition>();
            for (var current = target; current != start; current = cameFrom[current])
            {
                steps.Add(current);
            }

            steps.Reverse();
            return steps.AsReadOnly();
        }
    }
}
