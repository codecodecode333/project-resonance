using System;
using Riftchord.Core;
using Riftchord.Units;

namespace Riftchord.Grid
{
    public sealed class GridTraversal
    {
        private const int MaximumHeightDifference = 1;

        private readonly GridState _gridState;
        private readonly GridOccupancy _gridOccupancy;
        private readonly UnitRegistry _unitRegistry;

        public GridTraversal(
            GridState gridState,
            GridOccupancy gridOccupancy,
            UnitRegistry unitRegistry)
        {
            _gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            _gridOccupancy = gridOccupancy ?? throw new ArgumentNullException(nameof(gridOccupancy));
            _unitRegistry = unitRegistry ?? throw new ArgumentNullException(nameof(unitRegistry));
        }

        public bool CanPassThrough(
            UnitState mover,
            GridPosition from,
            GridPosition to)
        {
            if (mover == null
                || !_gridState.IsInBounds(from)
                || !_gridState.IsInBounds(to)
                || !AreOrthogonallyAdjacent(from, to))
            {
                return false;
            }

            var fromCell = _gridState.GetCell(from);
            var toCell = _gridState.GetCell(to);

            if (!toCell.IsWalkable
                || Math.Abs(fromCell.Height - toCell.Height) > MaximumHeightDifference)
            {
                return false;
            }

            if (!_gridOccupancy.TryGetOccupant(to, out var occupantId))
            {
                return true;
            }

            if (occupantId == mover.Id
                || !_unitRegistry.TryGetUnit(occupantId, out var occupant))
            {
                return false;
            }

            return occupant.Team == mover.Team;
        }

        public bool CanStopAt(UnitState mover, GridPosition position)
        {
            return mover != null
                && _gridState.IsInBounds(position)
                && _gridState.GetCell(position).IsWalkable
                && !_gridOccupancy.IsOccupied(position);
        }

        private static bool AreOrthogonallyAdjacent(GridPosition from, GridPosition to)
        {
            var deltaX = Math.Abs(from.X - to.X);
            var deltaZ = Math.Abs(from.Z - to.Z);
            return deltaX + deltaZ == 1;
        }
    }
}
