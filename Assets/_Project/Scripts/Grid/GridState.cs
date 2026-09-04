using System;
using System.Collections.Generic;
using Riftchord.Core;

namespace Riftchord.Grid
{
    public sealed class GridState
    {
        private readonly CellState[,] _cells;

        public GridState(int width, int depth)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be greater than zero.");
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    "Depth must be greater than zero.");
            }

            Width = width;
            Depth = depth;
            _cells = new CellState[width, depth];

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    _cells[x, z] = new CellState(new GridPosition(x, z));
                }
            }
        }

        public int Width { get; }

        public int Depth { get; }

        public int CellCount => _cells.Length;

        public bool IsInBounds(GridPosition position)
        {
            return position.X >= 0
                && position.X < Width
                && position.Z >= 0
                && position.Z < Depth;
        }

        public CellState GetCell(GridPosition position)
        {
            EnsureInBounds(position);
            return _cells[position.X, position.Z];
        }

        public bool TryGetCell(GridPosition position, out CellState cell)
        {
            if (!IsInBounds(position))
            {
                cell = null;
                return false;
            }

            cell = _cells[position.X, position.Z];
            return true;
        }

        public IEnumerable<CellState> GetOrthogonalNeighbors(GridPosition position)
        {
            EnsureInBounds(position);

            if (position.X + 1 < Width)
            {
                yield return _cells[position.X + 1, position.Z];
            }

            if (position.X - 1 >= 0)
            {
                yield return _cells[position.X - 1, position.Z];
            }

            if (position.Z + 1 < Depth)
            {
                yield return _cells[position.X, position.Z + 1];
            }

            if (position.Z - 1 >= 0)
            {
                yield return _cells[position.X, position.Z - 1];
            }
        }

        public void SetHeight(GridPosition position, int height)
        {
            GetCell(position).SetHeight(height);
        }

        public void SetWalkable(GridPosition position, bool isWalkable)
        {
            GetCell(position).SetWalkable(isWalkable);
        }

        private void EnsureInBounds(GridPosition position)
        {
            if (!IsInBounds(position))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    $"Position {position} is outside a {Width}x{Depth} grid.");
            }
        }
    }
}
