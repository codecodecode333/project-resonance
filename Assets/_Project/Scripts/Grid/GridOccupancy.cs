using System.Collections.Generic;
using ProjectResonance.Core;

namespace ProjectResonance.Grid
{
    public sealed class GridOccupancy
    {
        private readonly Dictionary<GridPosition, EntityId> _occupantsByPosition =
            new Dictionary<GridPosition, EntityId>();

        private readonly Dictionary<EntityId, GridPosition> _positionsByEntity =
            new Dictionary<EntityId, GridPosition>();

        public bool IsOccupied(GridPosition position)
        {
            return _occupantsByPosition.ContainsKey(position);
        }

        public bool TryGetOccupant(GridPosition position, out EntityId entityId)
        {
            return _occupantsByPosition.TryGetValue(position, out entityId);
        }

        public bool TryGetPosition(EntityId entityId, out GridPosition position)
        {
            return _positionsByEntity.TryGetValue(entityId, out position);
        }

        public bool TryOccupy(GridPosition position, EntityId entityId)
        {
            if (entityId.Value <= 0
                || _occupantsByPosition.ContainsKey(position)
                || _positionsByEntity.ContainsKey(entityId))
            {
                return false;
            }

            _occupantsByPosition.Add(position, entityId);
            _positionsByEntity.Add(entityId, position);
            return true;
        }

        public bool TryRelocate(EntityId entityId, GridPosition from, GridPosition to)
        {
            if (entityId.Value <= 0
                || from == to
                || !_occupantsByPosition.TryGetValue(from, out var currentOccupant)
                || currentOccupant != entityId
                || !_positionsByEntity.TryGetValue(entityId, out var occupiedPosition)
                || occupiedPosition != from
                || _occupantsByPosition.ContainsKey(to))
            {
                return false;
            }

            // All rejection paths precede mutation; this primitive has no traversal rules.
            _occupantsByPosition.Add(to, entityId);
            _occupantsByPosition.Remove(from);
            _positionsByEntity[entityId] = to;
            return true;
        }

        public bool TryRelease(GridPosition position, EntityId entityId)
        {
            if (!_occupantsByPosition.TryGetValue(position, out var currentOccupant)
                || currentOccupant != entityId
                || !_positionsByEntity.TryGetValue(entityId, out var occupiedPosition)
                || occupiedPosition != position)
            {
                return false;
            }

            _occupantsByPosition.Remove(position);
            _positionsByEntity.Remove(entityId);
            return true;
        }
    }
}
