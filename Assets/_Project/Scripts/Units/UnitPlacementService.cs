using System;
using ProjectResonance.Core;
using ProjectResonance.Grid;

namespace ProjectResonance.Units
{
    public sealed class UnitPlacementService
    {
        private readonly GridState _gridState;
        private readonly GridOccupancy _gridOccupancy;

        public UnitPlacementService(GridState gridState, GridOccupancy gridOccupancy)
        {
            _gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            _gridOccupancy = gridOccupancy ?? throw new ArgumentNullException(nameof(gridOccupancy));
        }

        public bool TryPlace(UnitState unit, GridPosition position)
        {
            if (unit == null
                || unit.IsPlaced
                || !_gridState.IsInBounds(position)
                || !_gridState.GetCell(position).IsWalkable
                || _gridOccupancy.IsOccupied(position)
                || _gridOccupancy.TryGetPosition(unit.Id, out _))
            {
                return false;
            }

            if (!_gridOccupancy.TryOccupy(position, unit.Id))
            {
                return false;
            }

            unit.PlaceAt(position);
            return true;
        }

        public bool TryRemove(UnitState unit)
        {
            if (unit == null || !unit.IsPlaced)
            {
                return false;
            }

            var position = unit.Position;

            if (!_gridOccupancy.TryGetOccupant(position, out var occupantId)
                || occupantId != unit.Id
                || !_gridOccupancy.TryGetPosition(unit.Id, out var occupiedPosition)
                || occupiedPosition != position
                || !_gridOccupancy.TryRelease(position, unit.Id))
            {
                return false;
            }

            unit.RemoveFromGrid();
            return true;
        }
    }
}
