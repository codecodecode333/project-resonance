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
    public sealed class UnitMovementTests
    {
        private GridState _grid;
        private GridOccupancy _occupancy;
        private UnitRegistry _registry;
        private UnitPlacementService _placement;
        private UnitMovementService _movement;
        private readonly List<UnitState> _units = new List<UnitState>();

        [Test]
        public void RelocateUpdatesBothMappingsWithoutApplyingGridRules()
        {
            var occupancy = new GridOccupancy();
            var id = new EntityId(1);
            var otherId = new EntityId(2);
            var start = new GridPosition(-1, 0);
            var target = new GridPosition(100, 5);
            var otherPosition = new GridPosition(0, 0);
            Assert.That(occupancy.TryOccupy(start, id), Is.True);
            Assert.That(occupancy.TryOccupy(otherPosition, otherId), Is.True);

            Assert.That(occupancy.TryRelocate(id, start, target), Is.True);

            Assert.That(occupancy.IsOccupied(start), Is.False);
            Assert.That(occupancy.TryGetOccupant(target, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(id));
            Assert.That(occupancy.TryGetPosition(id, out var position), Is.True);
            Assert.That(position, Is.EqualTo(target));
            Assert.That(occupancy.TryGetOccupant(otherPosition, out occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(otherId));
            Assert.That(occupancy.TryGetPosition(otherId, out position), Is.True);
            Assert.That(position, Is.EqualTo(otherPosition));
        }

        [Test]
        public void RejectedRelocationsPreserveEveryMapping()
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var other = PlaceUnit(2, UnitTeam.Enemy, new GridPosition(1, 0));
            var empty = new GridPosition(2, 0);
            var target = new GridPosition(3, 0);
            var missingId = new EntityId(99);

            AssertStateUnchanged(() =>
            {
                Assert.That(_occupancy.TryRelocate(mover.Id, mover.Position, other.Position), Is.False);
                Assert.That(_occupancy.TryRelocate(mover.Id, empty, target), Is.False);
                Assert.That(_occupancy.TryRelocate(mover.Id, other.Position, target), Is.False);
                Assert.That(_occupancy.TryRelocate(other.Id, mover.Position, target), Is.False);
                Assert.That(_occupancy.TryRelocate(mover.Id, mover.Position, mover.Position), Is.False);
                Assert.That(_occupancy.TryRelocate(default, mover.Position, target), Is.False);
                Assert.That(_occupancy.TryRelocate(missingId, mover.Position, target), Is.False);
                Assert.That(_occupancy.TryGetPosition(default, out _), Is.False);
                Assert.That(_occupancy.TryGetPosition(missingId, out _), Is.False);
            });
        }

        [TestCase(1)]
        [TestCase(3)]
        public void MoveAppliesOnlyTheFinalPositionAndReturnsOrderedRoute(int targetX)
        {
            CreateGrid(4, 1);
            var start = new GridPosition(0, 0);
            var mover = PlaceUnit(1, UnitTeam.Player, start);
            var target = new GridPosition(targetX, 0);
            _grid.SetHeight(new GridPosition(1, 0), 1);

            Assert.That(_movement.TryMove(mover, target, targetX, out var path), Is.True);

            Assert.That(path, Is.EqualTo(Enumerable.Range(1, targetX)
                .Select(x => new GridPosition(x, 0)).ToArray()));
            AssertPlacedAt(mover, target);
            Assert.That(_occupancy.IsOccupied(start), Is.False);
            foreach (var step in path.Take(path.Count - 1))
            {
                Assert.That(_occupancy.IsOccupied(step), Is.False);
            }
        }

        [Test]
        public void AllyPassThroughPreservesAllyAndRelocatesMoverDirectlyToTarget()
        {
            CreateGrid(3, 1);
            var start = new GridPosition(0, 0);
            var allyPosition = new GridPosition(1, 0);
            var target = new GridPosition(2, 0);
            var mover = PlaceUnit(1, UnitTeam.Player, start);
            var ally = PlaceUnit(2, UnitTeam.Player, allyPosition);

            Assert.That(_movement.TryMove(mover, target, 2, out var path), Is.True);

            Assert.That(path, Is.EqualTo(new[] { allyPosition, target }));
            AssertPlacedAt(mover, target);
            AssertPlacedAt(ally, allyPosition);
            Assert.That(_occupancy.IsOccupied(start), Is.False);
        }

        [TestCase("Enemy")]
        [TestCase("Unknown")]
        [TestCase("OccupiedTarget")]
        [TestCase("Height")]
        [TestCase("Range")]
        [TestCase("ZeroDistance")]
        [TestCase("Unwalkable")]
        [TestCase("Outside")]
        public void UnavailableMovesReturnEmptyPathAndPreserveAllState(string reason)
        {
            CreateGrid(6, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            PlaceUnit(2, UnitTeam.Player, new GridPosition(4, 0));
            Assert.That(_occupancy.TryOccupy(new GridPosition(5, 0), new EntityId(99)), Is.True);
            var target = new GridPosition(3, 0);
            var barrier = new GridPosition(1, 0);
            var maxDistance = 3;
            switch (reason)
            {
                case "Enemy":
                    PlaceUnit(3, UnitTeam.Enemy, barrier);
                    break;
                case "Unknown":
                    Assert.That(_occupancy.TryOccupy(barrier, new EntityId(98)), Is.True);
                    break;
                case "OccupiedTarget":
                    PlaceUnit(3, UnitTeam.Player, target);
                    break;
                case "Height":
                    _grid.SetHeight(barrier, 2);
                    break;
                case "Range":
                    maxDistance = 2;
                    break;
                case "ZeroDistance":
                    maxDistance = 0;
                    break;
                case "Unwalkable":
                    _grid.SetWalkable(barrier, false);
                    break;
                case "Outside":
                    target = new GridPosition(-1, 0);
                    break;
            }

            AssertStateUnchanged(() =>
            {
                Assert.That(_movement.TryMove(mover, target, maxDistance, out var path), Is.False);
                Assert.That(path, Is.Empty);
            });
        }

        [Test]
        public void SameTargetIsASuccessfulEmptyNoOp()
        {
            CreateGrid(2, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            PlaceUnit(2, UnitTeam.Enemy, new GridPosition(1, 0));

            AssertStateUnchanged(() =>
            {
                Assert.That(_movement.TryMove(mover, mover.Position, 0, out var path), Is.True);
                Assert.That(path, Is.Empty);
            });
        }

        [Test]
        public void InvalidInputsThrowWithoutChangingStateEvenForNoOpRequests()
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var unplaced = new UnitState(new EntityId(2), UnitTeam.Player);
            _units.Add(unplaced);
            var outside = PlaceUnit(3, UnitTeam.Player, new GridPosition(3, 0));
            var smallerGrid = new GridState(3, 1);
            var smallerFinder = new PathFinder(smallerGrid, new GridTraversal(smallerGrid, _occupancy, _registry));
            var smallerMovement = new UnitMovementService(_occupancy, smallerFinder);

            AssertStateUnchanged(() =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                    _movement.TryMove(null, mover.Position, 0, out _));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    _movement.TryMove(mover, mover.Position, -1, out _));
                Assert.Throws<InvalidOperationException>(() =>
                    _movement.TryMove(unplaced, mover.Position, 0, out _));
                Assert.Throws<InvalidOperationException>(() =>
                    smallerMovement.TryMove(outside, outside.Position, 0, out _));
            });
        }

        [TestCase("Missing")]
        [TestCase("Relocated")]
        [TestCase("Replaced")]
        public void BrokenSpatialStateThrowsBeforeQueryOrNoOpAndIsNotSilentlyRepaired(string damage)
        {
            CreateGrid(4, 1);
            var start = new GridPosition(0, 0);
            var mover = PlaceUnit(1, UnitTeam.Player, start);
            PlaceUnit(2, UnitTeam.Enemy, new GridPosition(3, 0));
            if (damage == "Relocated")
            {
                Assert.That(_occupancy.TryRelocate(mover.Id, start, new GridPosition(1, 0)), Is.True);
            }
            else
            {
                Assert.That(_occupancy.TryRelease(start, mover.Id), Is.True);
                if (damage == "Replaced")
                {
                    Assert.That(_occupancy.TryOccupy(start, new EntityId(99)), Is.True);
                }
            }

            AssertStateUnchanged(() =>
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _movement.TryMove(mover, new GridPosition(2, 0), 2, out _));
                Assert.Throws<InvalidOperationException>(() =>
                    _movement.TryMove(mover, start, 0, out _));
            });
        }

        [Test]
        public void RelocationRejectionAfterPathSuccessDoesNotMoveUnitOrExposePath()
        {
            CreateGrid(2, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(1, 0);
            PlaceUnit(2, UnitTeam.Enemy, target);

            // Deliberately mismatched query occupancy exercises the final relocation safety gate.
            var emptyQueryOccupancy = new GridOccupancy();
            var finder = new PathFinder(_grid, new GridTraversal(_grid, emptyQueryOccupancy, _registry));
            var movement = new UnitMovementService(_occupancy, finder);
            Assert.That(finder.TryFindPath(mover, target, 1, out var validatedPath), Is.True);
            Assert.That(validatedPath, Is.EqualTo(new[] { target }));

            AssertStateUnchanged(() =>
            {
                Assert.That(movement.TryMove(mover, target, 1, out var path), Is.False);
                Assert.That(path, Is.Empty);
            });
        }

        private void CreateGrid(int width, int depth)
        {
            _units.Clear();
            _grid = new GridState(width, depth);
            _occupancy = new GridOccupancy();
            _registry = new UnitRegistry();
            _placement = new UnitPlacementService(_grid, _occupancy);
            var finder = new PathFinder(_grid, new GridTraversal(_grid, _occupancy, _registry));
            _movement = new UnitMovementService(_occupancy, finder);
        }

        private UnitState PlaceUnit(int id, UnitTeam team, GridPosition position)
        {
            var unit = new UnitState(new EntityId(id), team);
            Assert.That(_registry.Register(unit), Is.True);
            Assert.That(_placement.TryPlace(unit, position), Is.True);
            _units.Add(unit);
            return unit;
        }

        private void AssertPlacedAt(UnitState unit, GridPosition expected)
        {
            Assert.That(unit.IsPlaced, Is.True);
            Assert.That(unit.Position, Is.EqualTo(expected));
            Assert.That(_occupancy.TryGetPosition(unit.Id, out var position), Is.True);
            Assert.That(position, Is.EqualTo(expected));
            Assert.That(_occupancy.TryGetOccupant(expected, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(unit.Id));
            Assert.That(_registry.TryGetUnit(unit.Id, out var registered), Is.True);
            Assert.That(registered, Is.SameAs(unit));
        }

        private void AssertStateUnchanged(Action action)
        {
            var cells = Enumerable.Range(0, _grid.Width)
                .SelectMany(x => Enumerable.Range(0, _grid.Depth).Select(z => _grid.GetCell(new GridPosition(x, z))))
                .ToArray();
            var terrainBefore = cells.Select(cell => (cell.Position, cell.Height, cell.IsWalkable)).ToArray();
            var occupancyBefore = cells.Select(cell =>
                _occupancy.TryGetOccupant(cell.Position, out var id) ? id : default).ToArray();
            var unitsBefore = _units.Select(unit =>
                (unit.Id, unit.Team, unit.IsPlaced, Position: unit.IsPlaced ? (GridPosition?)unit.Position : null))
                .ToArray();
            var ids = _units.Select(unit => unit.Id).Concat(occupancyBefore)
                .Where(id => id.Value > 0).Distinct().ToArray();
            var reverseBefore = ids.Select(id =>
                _occupancy.TryGetPosition(id, out var position) ? (GridPosition?)position : null).ToArray();
            var registryBefore = ids.Select(id =>
                _registry.TryGetUnit(id, out var unit) ? unit : null).ToArray();

            action();

            Assert.That(cells.Select(cell => (cell.Position, cell.Height, cell.IsWalkable)), Is.EqualTo(terrainBefore));
            Assert.That(cells.Select(cell =>
                _occupancy.TryGetOccupant(cell.Position, out var id) ? id : default), Is.EqualTo(occupancyBefore));
            Assert.That(_units.Select(unit =>
                (unit.Id, unit.Team, unit.IsPlaced, Position: unit.IsPlaced ? (GridPosition?)unit.Position : null)),
                Is.EqualTo(unitsBefore));
            Assert.That(ids.Select(id =>
                _occupancy.TryGetPosition(id, out var position) ? (GridPosition?)position : null), Is.EqualTo(reverseBefore));
            Assert.That(ids.Select(id =>
                _registry.TryGetUnit(id, out var unit) ? unit : null), Is.EqualTo(registryBefore));
        }
    }
}
