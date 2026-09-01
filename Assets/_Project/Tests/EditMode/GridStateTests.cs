using System;
using System.Linq;
using NUnit.Framework;
using ProjectResonance.Core;
using ProjectResonance.Grid;

namespace ProjectResonance.Tests.EditMode
{
    public sealed class GridStateTests
    {
        [Test]
        public void ConstructorCreatesExpectedGridAndDefaultCells()
        {
            var grid = new GridState(8, 10);
            var position = new GridPosition(3, 5);
            var cell = grid.GetCell(position);

            Assert.That(grid.Width, Is.EqualTo(8));
            Assert.That(grid.Depth, Is.EqualTo(10));
            Assert.That(grid.CellCount, Is.EqualTo(80));
            Assert.That(cell.Position, Is.EqualTo(position));
            Assert.That(cell.Height, Is.Zero);
            Assert.That(cell.IsWalkable, Is.True);
        }

        [TestCase(0, 10)]
        [TestCase(-1, 10)]
        [TestCase(8, 0)]
        [TestCase(8, -1)]
        public void ConstructorRejectsNonPositiveDimensions(int width, int depth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridState(width, depth));
        }

        [Test]
        public void IsInBoundsRecognizesEdgesAndRejectsOutsidePositions()
        {
            var grid = new GridState(8, 10);

            Assert.That(grid.IsInBounds(new GridPosition(0, 0)), Is.True);
            Assert.That(grid.IsInBounds(new GridPosition(7, 9)), Is.True);
            Assert.That(grid.IsInBounds(new GridPosition(-1, 0)), Is.False);
            Assert.That(grid.IsInBounds(new GridPosition(8, 0)), Is.False);
            Assert.That(grid.IsInBounds(new GridPosition(0, -1)), Is.False);
            Assert.That(grid.IsInBounds(new GridPosition(0, 10)), Is.False);
        }

        [Test]
        public void CellLookupHandlesValidAndInvalidPositionsExplicitly()
        {
            var grid = new GridState(2, 2);
            var validPosition = new GridPosition(1, 1);
            var invalidPosition = new GridPosition(2, 1);

            Assert.That(grid.GetCell(validPosition).Position, Is.EqualTo(validPosition));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(invalidPosition));
            Assert.That(grid.TryGetCell(validPosition, out var validCell), Is.True);
            Assert.That(validCell.Position, Is.EqualTo(validPosition));
            Assert.That(grid.TryGetCell(invalidPosition, out var invalidCell), Is.False);
            Assert.That(invalidCell, Is.Null);
        }

        [Test]
        public void CellRuntimeStateAcceptsValidChangesAndRejectsInvalidHeight()
        {
            var grid = new GridState(1, 1);
            var position = new GridPosition(0, 0);

            foreach (var height in new[] { 0, 1, 2 })
            {
                grid.SetHeight(position, height);
                Assert.That(grid.GetCell(position).Height, Is.EqualTo(height));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetHeight(position, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetHeight(position, 3));
            Assert.That(grid.GetCell(position).Height, Is.EqualTo(2));

            grid.SetWalkable(position, false);
            Assert.That(grid.GetCell(position).IsWalkable, Is.False);
        }

        [Test]
        public void OrthogonalNeighborsReturnSpatialNeighborsWithoutTraversalFiltering()
        {
            var grid = new GridState(5, 5);
            var center = new GridPosition(2, 2);
            var highUnwalkableNeighbor = new GridPosition(3, 2);
            grid.SetHeight(highUnwalkableNeighbor, 2);
            grid.SetWalkable(highUnwalkableNeighbor, false);

            var neighbors = grid.GetOrthogonalNeighbors(center)
                .Select(cell => cell.Position)
                .ToArray();

            Assert.That(neighbors, Is.EqualTo(new[]
            {
                new GridPosition(3, 2),
                new GridPosition(1, 2),
                new GridPosition(2, 3),
                new GridPosition(2, 1),
            }));
        }

        [Test]
        public void CornerHasTwoOrthogonalNeighbors()
        {
            var grid = new GridState(5, 5);

            var neighbors = grid.GetOrthogonalNeighbors(new GridPosition(0, 0))
                .Select(cell => cell.Position)
                .ToArray();

            Assert.That(neighbors, Is.EqualTo(new[]
            {
                new GridPosition(1, 0),
                new GridPosition(0, 1),
            }));
        }

        [Test]
        public void GridPositionSupportsValueEqualityHashingAndDebugText()
        {
            var first = new GridPosition(3, 5);
            var same = new GridPosition(3, 5);
            var different = new GridPosition(5, 3);

            Assert.That(first == same, Is.True);
            Assert.That(first != different, Is.True);
            Assert.That(first.Equals(same), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo("(3, 5)"));
        }
    }
}
