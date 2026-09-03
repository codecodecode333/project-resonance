using System;
using System.Collections.Generic;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using ProjectResonance.Units;

namespace ProjectResonance.Movement
{
    public sealed class UnitMovementService
    {
        private readonly GridOccupancy _occupancy;
        private readonly PathFinder _pathFinder;

        public UnitMovementService(GridOccupancy occupancy, PathFinder pathFinder)
        {
            _occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy));
            _pathFinder = pathFinder ?? throw new ArgumentNullException(nameof(pathFinder));
        }

        /// <summary>
        /// Validates a route, then relocates the unit and occupancy directly to the target.
        /// The returned route excludes start and includes target; domain movement is already complete.
        /// Intermediate cells are never occupied by the mover. Same-target movement is an empty no-op.
        /// Normal failure returns false with an empty path and no state changes; invalid state throws.
        /// </summary>
        public bool TryMove(
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

            if (!mover.IsPlaced)
            {
                throw new InvalidOperationException("Mover must be placed before moving.");
            }

            var start = mover.Position;
            if (!_occupancy.TryGetPosition(mover.Id, out var occupiedPosition)
                || occupiedPosition != start
                || !_occupancy.TryGetOccupant(start, out var occupantId)
                || occupantId != mover.Id)
            {
                throw new InvalidOperationException($"Unit {mover.Id} position and grid occupancy are inconsistent.");
            }

            if (!_pathFinder.TryFindPath(mover, target, maxDistance, out var validatedPath))
            {
                return false;
            }

            if (start == target)
            {
                path = validatedPath;
                return true;
            }

            if (!_occupancy.TryRelocate(mover.Id, start, target))
            {
                return false;
            }

            // Main-thread synchronous domain: the pure query and relocation have no callbacks.
            // The unit is therefore still at the validated start, so this guarded assignment succeeds.
            mover.MoveFromTo(start, target);
            path = validatedPath;
            return true;
        }
    }
}
