using System;
using NUnit.Framework;
using Riftchord.Core;
using Riftchord.Grid;
using Riftchord.Units;

namespace Riftchord.Tests.EditMode
{
    public sealed class UnitPlacementTests
    {
        [Test]
        public void EntityIdSupportsValueEqualityHashingAndDebugText()
        {
            var first = new EntityId(7);
            var same = new EntityId(7);
            var different = new EntityId(8);

            Assert.That(first.Value, Is.EqualTo(7));
            Assert.That(first == same, Is.True);
            Assert.That(first != different, Is.True);
            Assert.That(first.Equals(same), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo("EntityId(7)"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EntityIdRejectsNonPositiveValues(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EntityId(value));
        }

        [Test]
        public void NewUnitStartsUnplaced()
        {
            var unit = CreateUnit(1, UnitTeam.Player);

            Assert.That(unit.Id, Is.EqualTo(new EntityId(1)));
            Assert.That(unit.Team, Is.EqualTo(UnitTeam.Player));
            Assert.That(unit.IsPlaced, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = unit.Position);
        }

        [Test]
        public void TryPlaceSucceedsOnWalkableUnoccupiedCell()
        {
            var grid = new GridState(3, 3);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var target = new GridPosition(1, 2);

            Assert.That(placement.TryPlace(unit, target), Is.True);
            Assert.That(unit.IsPlaced, Is.True);
            Assert.That(unit.Position, Is.EqualTo(target));
            Assert.That(occupancy.TryGetOccupant(target, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(unit.Id));
            Assert.That(occupancy.TryGetPosition(unit.Id, out var occupiedPosition), Is.True);
            Assert.That(occupiedPosition, Is.EqualTo(target));
        }

        [Test]
        public void TryPlaceRejectsOutOfBoundsWithoutChangingState()
        {
            var grid = new GridState(2, 2);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var outside = new GridPosition(2, 0);

            Assert.That(placement.TryPlace(unit, outside), Is.False);
            Assert.That(unit.IsPlaced, Is.False);
            Assert.That(occupancy.IsOccupied(outside), Is.False);
            Assert.That(occupancy.TryGetPosition(unit.Id, out _), Is.False);
        }

        [Test]
        public void TryPlaceRejectsUnwalkableTerrainWithoutChangingState()
        {
            var grid = new GridState(2, 2);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var target = new GridPosition(1, 1);
            grid.SetWalkable(target, false);

            Assert.That(placement.TryPlace(unit, target), Is.False);
            Assert.That(unit.IsPlaced, Is.False);
            Assert.That(occupancy.IsOccupied(target), Is.False);
            Assert.That(occupancy.TryGetPosition(unit.Id, out _), Is.False);
        }

        [Test]
        public void TryPlaceRejectsCellOccupiedByAnotherUnit()
        {
            var grid = new GridState(2, 2);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var firstUnit = CreateUnit(1, UnitTeam.Player);
            var secondUnit = CreateUnit(2, UnitTeam.Enemy);
            var target = new GridPosition(1, 1);
            Assert.That(placement.TryPlace(firstUnit, target), Is.True);

            Assert.That(placement.TryPlace(secondUnit, target), Is.False);
            Assert.That(secondUnit.IsPlaced, Is.False);
            Assert.That(occupancy.TryGetOccupant(target, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(firstUnit.Id));
        }

        [Test]
        public void TryPlaceRejectsAlreadyPlacedUnitAtAnotherCell()
        {
            var grid = new GridState(3, 3);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var firstPosition = new GridPosition(0, 0);
            var secondPosition = new GridPosition(1, 0);
            Assert.That(placement.TryPlace(unit, firstPosition), Is.True);

            Assert.That(placement.TryPlace(unit, secondPosition), Is.False);
            Assert.That(unit.Position, Is.EqualTo(firstPosition));
            Assert.That(occupancy.IsOccupied(secondPosition), Is.False);
            Assert.That(occupancy.TryGetPosition(unit.Id, out var occupiedPosition), Is.True);
            Assert.That(occupiedPosition, Is.EqualTo(firstPosition));
        }

        [Test]
        public void GridOccupancyPreservesOneToOneMappingAndRejectsWrongRelease()
        {
            var occupancy = new GridOccupancy();
            var firstPosition = new GridPosition(0, 0);
            var secondPosition = new GridPosition(1, 0);
            var firstEntity = new EntityId(1);
            var secondEntity = new EntityId(2);

            Assert.That(occupancy.TryOccupy(firstPosition, firstEntity), Is.True);
            Assert.That(occupancy.TryOccupy(secondPosition, firstEntity), Is.False);
            Assert.That(occupancy.TryOccupy(firstPosition, secondEntity), Is.False);
            Assert.That(occupancy.TryRelease(firstPosition, secondEntity), Is.False);
            Assert.That(occupancy.TryGetOccupant(firstPosition, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(firstEntity));
            Assert.That(occupancy.TryGetPosition(firstEntity, out var occupiedPosition), Is.True);
            Assert.That(occupiedPosition, Is.EqualTo(firstPosition));
        }

        [Test]
        public void TryRemoveClearsOccupancyAndUnitPlacement()
        {
            var grid = new GridState(2, 2);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var target = new GridPosition(1, 1);
            Assert.That(placement.TryPlace(unit, target), Is.True);

            Assert.That(placement.TryRemove(unit), Is.True);
            Assert.That(unit.IsPlaced, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = unit.Position);
            Assert.That(occupancy.IsOccupied(target), Is.False);
            Assert.That(occupancy.TryGetPosition(unit.Id, out _), Is.False);
        }

        [Test]
        public void TryRemoveRejectsMismatchedOccupancyWithoutDeletingOtherEntity()
        {
            var grid = new GridState(2, 2);
            var occupancy = new GridOccupancy();
            var placement = new UnitPlacementService(grid, occupancy);
            var unit = CreateUnit(1, UnitTeam.Player);
            var otherEntity = new EntityId(2);
            var target = new GridPosition(1, 1);
            Assert.That(placement.TryPlace(unit, target), Is.True);
            Assert.That(occupancy.TryRelease(target, unit.Id), Is.True);
            Assert.That(occupancy.TryOccupy(target, otherEntity), Is.True);

            Assert.That(placement.TryRemove(unit), Is.False);
            Assert.That(unit.IsPlaced, Is.True);
            Assert.That(unit.Position, Is.EqualTo(target));
            Assert.That(occupancy.TryGetOccupant(target, out var occupant), Is.True);
            Assert.That(occupant, Is.EqualTo(otherEntity));
        }

        private static UnitState CreateUnit(int id, UnitTeam team)
        {
            return new UnitState(new EntityId(id), team);
        }
    }
}
