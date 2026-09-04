using System;
using System.Collections.Generic;
using Riftchord.Core;
using Riftchord.Grid;
using Riftchord.Units;

namespace Riftchord.Movement
{
    public sealed class ReachabilityFinder
    {
        private readonly GridState _gridState;
        private readonly GridTraversal _gridTraversal;

        public ReachabilityFinder(GridState gridState, GridTraversal gridTraversal)
        {
            _gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            _gridTraversal = gridTraversal ?? throw new ArgumentNullException(nameof(gridTraversal));
        }

        /// <summary>
        /// Returns stoppable destinations within maxDistance unit-cost steps, excluding the start.
        /// Result order is unspecified. This query does not change game state.
        /// </summary>
        public IReadOnlyCollection<GridPosition> FindReachableCells(UnitState mover, int maxDistance)
        {
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

            if (maxDistance == 0)
            {
                return Array.Empty<GridPosition>();
            }

            var start = mover.Position;
            var queue = new Queue<GridPosition>();
            var distances = new Dictionary<GridPosition, int> { { start, 0 } };
            var reachable = new List<GridPosition>();
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
                    // With unit edge costs, the first discovery is the shortest distance.
                    if (distances.ContainsKey(position)
                        || !_gridTraversal.CanPassThrough(mover, current, position))
                    {
                        continue;
                    }

                    distances.Add(position, nextDistance);
                    queue.Enqueue(position);

                    // A pass-through cell can expand the search without being a destination.
                    if (_gridTraversal.CanStopAt(mover, position))
                    {
                        reachable.Add(position);
                    }
                }
            }

            return reachable.AsReadOnly();
        }
    }
}
