using System;
using ProjectResonance.Core;

namespace ProjectResonance.Grid
{
    public sealed class CellState
    {
        private const int MinimumHeight = 0;
        private const int MaximumHeight = 2;

        internal CellState(GridPosition position)
        {
            Position = position;
            Height = MinimumHeight;
            IsWalkable = true;
        }

        public GridPosition Position { get; }

        public int Height { get; private set; }

        public bool IsWalkable { get; private set; }

        internal void SetHeight(int height)
        {
            if (height < MinimumHeight || height > MaximumHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    $"Height must be between {MinimumHeight} and {MaximumHeight}.");
            }

            Height = height;
        }

        internal void SetWalkable(bool isWalkable)
        {
            IsWalkable = isWalkable;
        }
    }
}
