using System;
using Riftchord.Core;

namespace Riftchord.Units
{
    public sealed class UnitState
    {
        private GridPosition? _position;

        public UnitState(EntityId id, UnitTeam team)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentException("A unit requires a valid EntityId.", nameof(id));
            }

            if (team != UnitTeam.Player && team != UnitTeam.Enemy)
            {
                throw new ArgumentOutOfRangeException(nameof(team), team, "Unsupported unit team.");
            }

            Id = id;
            Team = team;
        }

        public EntityId Id { get; }

        public UnitTeam Team { get; }

        public bool IsPlaced => _position.HasValue;

        public GridPosition Position
        {
            get
            {
                if (!_position.HasValue)
                {
                    throw new InvalidOperationException($"Unit {Id} is not placed.");
                }

                return _position.Value;
            }
        }

        internal void PlaceAt(GridPosition position)
        {
            if (IsPlaced)
            {
                throw new InvalidOperationException($"Unit {Id} is already placed.");
            }

            _position = position;
        }

        internal void MoveFromTo(GridPosition expectedFrom, GridPosition target)
        {
            if (!_position.HasValue || _position.Value != expectedFrom)
            {
                throw new InvalidOperationException($"Unit {Id} is not placed at expected position {expectedFrom}.");
            }

            _position = target;
        }

        internal void RemoveFromGrid()
        {
            if (!IsPlaced)
            {
                throw new InvalidOperationException($"Unit {Id} is not placed.");
            }

            _position = null;
        }
    }
}
