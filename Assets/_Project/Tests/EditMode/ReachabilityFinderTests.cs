using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Riftchord.Core;
using Riftchord.Grid;
using Riftchord.Movement;
using Riftchord.Units;

namespace Riftchord.Tests.EditMode
{
    public sealed class ReachabilityFinderTests
    {
        private GridState _grid;
        private GridOccupancy _occupancy;
        private UnitRegistry _registry;
        private UnitPlacementService _placement;
        private ReachabilityFinder _finder;

        [Test]
        public void ZeroDistanceReturnsNoDestinationsEvenAfterAnotherQuery()
        {
            CreateGrid(5, 5);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(2, 2));

            Assert.That(_finder.FindReachableCells(mover, 2), Is.Not.Empty);
            Assert.That(_finder.FindReachableCells(mover, 0), Is.Empty);
        }

        [Test]
        public void InvalidQueriesFailExplicitlyBeforeSearching()
        {
            CreateGrid(5, 5);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(2, 2));
            var unplaced = new UnitState(new EntityId(2), UnitTeam.Player);

            Assert.Throws<ArgumentNullException>(() => _finder.FindReachableCells(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _finder.FindReachableCells(mover, -1));
            Assert.Throws<InvalidOperationException>(() => _finder.FindReachableCells(unplaced, 0));
            Assert.Throws<InvalidOperationException>(() => _finder.FindReachableCells(unplaced, 1));

            var outside = new UnitState(new EntityId(3), UnitTeam.Player);
            var otherPlacement = new UnitPlacementService(new GridState(6, 1), new GridOccupancy());
            Assert.That(otherPlacement.TryPlace(outside, new GridPosition(5, 0)), Is.True);
            Assert.Throws<InvalidOperationException>(() => _finder.FindReachableCells(outside, 0));
            Assert.Throws<InvalidOperationException>(() => _finder.FindReachableCells(outside, 1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public void EmptyGridReturnsManhattanDestinationsWithoutStartOrDuplicates(int maxDistance)
        {
            CreateGrid(5, 5);
            var start = new GridPosition(2, 2);
            var mover = PlaceUnit(1, UnitTeam.Player, start);
            var expected = AllPositions()
                .Where(position => position != start
                    && Math.Abs(position.X - start.X) + Math.Abs(position.Z - start.Z) <= maxDistance)
                .ToArray();

            var reachable = _finder.FindReachableCells(mover, maxDistance);

            Assert.That(reachable, Is.EquivalentTo(expected));
        }

        [TestCase(false, 0)]
        [TestCase(true, 2)]
        public void TerrainBlockedCorridorDoesNotReachCellsBeyondBarrier(bool walkable, int height)
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var barrier = new GridPosition(1, 0);
            _grid.SetWalkable(barrier, walkable);
            _grid.SetHeight(barrier, height);

            Assert.That(_finder.FindReachableCells(mover, 3), Is.Empty);
        }

        [Test]
        public void AllyIsTraversedButOnlyTheCellBeyondItIsADestination()
        {
            CreateGrid(3, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            PlaceUnit(2, UnitTeam.Player, new GridPosition(1, 0));

            Assert.That(_finder.FindReachableCells(mover, 1), Is.Empty);
            Assert.That(_finder.FindReachableCells(mover, 2), Is.EquivalentTo(new[]
            {
                new GridPosition(2, 0),
            }));
        }

        [TestCase(true, TestName = "EnemyBlocksCorridorAndCellsBeyondIt")]
        [TestCase(false, TestName = "UnknownEntityBlocksCorridorAndCellsBeyondIt")]
        public void BlockingOccupancyStopsCorridorExploration(bool knownEnemy)
        {
            CreateGrid(3, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var barrier = new GridPosition(1, 0);
            if (knownEnemy)
            {
                PlaceUnit(2, UnitTeam.Enemy, barrier);
            }
            else
            {
                Assert.That(_occupancy.TryOccupy(barrier, new EntityId(99)), Is.True);
            }

            Assert.That(_finder.FindReachableCells(mover, 2), Is.Empty);
        }

        [Test]
        public void HeightBlockedDirectEdgeDoesNotPreventAValidDetour()
        {
            CreateGrid(2, 2);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(1, 0);
            var firstStep = new GridPosition(0, 1);
            var secondStep = new GridPosition(1, 1);
            _grid.SetHeight(target, 2);
            _grid.SetHeight(firstStep, 1);
            _grid.SetHeight(secondStep, 2);

            Assert.That(_finder.FindReachableCells(mover, 2),
                Is.EquivalentTo(new[] { firstStep, secondStep }));
            Assert.That(_finder.FindReachableCells(mover, 3),
                Is.EquivalentTo(new[] { firstStep, secondStep, target }));
        }

        [Test]
        public void UphillAndDownhillEachConsumeOneStep()
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            _grid.SetHeight(new GridPosition(1, 0), 1);

            Assert.That(_finder.FindReachableCells(mover, 2), Is.EquivalentTo(new[]
            {
                new GridPosition(1, 0),
                new GridPosition(2, 0),
            }));
        }

        [Test]
        public void QueryPreservesUnitPlacementTerrainRegistryAndBothOccupancyMappings()
        {
            CreateGrid(5, 2);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var ally = PlaceUnit(2, UnitTeam.Player, new GridPosition(1, 0));
            var enemy = PlaceUnit(3, UnitTeam.Enemy, new GridPosition(4, 0));
            var unknownId = new EntityId(99);
            var unknownPosition = new GridPosition(3, 0);
            Assert.That(_occupancy.TryOccupy(unknownPosition, unknownId), Is.True);
            _grid.SetHeight(ally.Position, 1);
            _grid.SetWalkable(new GridPosition(3, 1), false);

            var cells = AllPositions().Select(position => _grid.GetCell(position)).ToArray();
            var terrainBefore = cells.Select(cell => (cell.Position, cell.Height, cell.IsWalkable)).ToArray();
            var occupancyBefore = cells.Select(cell =>
                _occupancy.TryGetOccupant(cell.Position, out var id) ? id : default).ToArray();
            var units = new[] { mover, ally, enemy };
            var positionsBefore = units.Select(unit => unit.Position).ToArray();

            Assert.That(_finder.FindReachableCells(mover, 4), Is.Not.Empty);

            Assert.That(cells.Select(cell => (cell.Position, cell.Height, cell.IsWalkable)),
                Is.EqualTo(terrainBefore));
            Assert.That(cells.Select(cell =>
                _occupancy.TryGetOccupant(cell.Position, out var id) ? id : default),
                Is.EqualTo(occupancyBefore));
            for (var i = 0; i < units.Length; i++)
            {
                Assert.That(units[i].IsPlaced, Is.True);
                Assert.That(units[i].Position, Is.EqualTo(positionsBefore[i]));
                Assert.That(_occupancy.TryGetPosition(units[i].Id, out var position), Is.True);
                Assert.That(position, Is.EqualTo(positionsBefore[i]));
                Assert.That(_registry.TryGetUnit(units[i].Id, out var registered), Is.True);
                Assert.That(registered, Is.SameAs(units[i]));
            }

            Assert.That(_occupancy.TryGetPosition(unknownId, out var unknownAfter), Is.True);
            Assert.That(unknownAfter, Is.EqualTo(unknownPosition));
            Assert.That(_registry.TryGetUnit(unknownId, out _), Is.False);
        }

        private void CreateGrid(int width, int depth)
        {
            _grid = new GridState(width, depth);
            _occupancy = new GridOccupancy();
            _registry = new UnitRegistry();
            _placement = new UnitPlacementService(_grid, _occupancy);
            _finder = new ReachabilityFinder(_grid, new GridTraversal(_grid, _occupancy, _registry));
        }

        private UnitState PlaceUnit(int id, UnitTeam team, GridPosition position)
        {
            var unit = new UnitState(new EntityId(id), team);
            Assert.That(_registry.Register(unit), Is.True);
            Assert.That(_placement.TryPlace(unit, position), Is.True);
            return unit;
        }

        private IEnumerable<GridPosition> AllPositions()
        {
            return Enumerable.Range(0, _grid.Width)
                .SelectMany(x => Enumerable.Range(0, _grid.Depth).Select(z => new GridPosition(x, z)));
        }
    }
}
