using System.Linq;
using NUnit.Framework;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using ProjectResonance.Units;

namespace ProjectResonance.Tests.EditMode
{
    public sealed class GridTraversalTests
    {
        [Test]
        public void UnitRegistryRegistersAndLooksUpUnitByEntityId()
        {
            var registry = new UnitRegistry();
            var unit = CreateUnit(1, UnitTeam.Player);

            Assert.That(registry.Register(unit), Is.True);
            Assert.That(registry.TryGetUnit(unit.Id, out var registered), Is.True);
            Assert.That(registered, Is.SameAs(unit));
            Assert.That(registry.TryGetUnit(new EntityId(2), out _), Is.False);
        }

        [Test]
        public void UnitRegistryRejectsNullAndDuplicateEntityIds()
        {
            var registry = new UnitRegistry();
            var first = CreateUnit(1, UnitTeam.Player);
            var duplicateId = CreateUnit(1, UnitTeam.Enemy);

            Assert.That(registry.Register(null), Is.False);
            Assert.That(registry.Register(first), Is.True);
            Assert.That(registry.Register(first), Is.False);
            Assert.That(registry.Register(duplicateId), Is.False);
            Assert.That(registry.TryGetUnit(first.Id, out var registered), Is.True);
            Assert.That(registered, Is.SameAs(first));
        }

        [Test]
        public void CanPassThroughRequiresAnOrthogonalStep()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var from = new GridPosition(1, 1);

            Assert.That(
                context.Traversal.CanPassThrough(mover, from, new GridPosition(2, 1)),
                Is.True);
            Assert.That(
                context.Traversal.CanPassThrough(mover, from, new GridPosition(2, 2)),
                Is.False);
        }

        [Test]
        public void CanPassThroughAllowsOneHeightLevelButRejectsTwo()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);

            context.Grid.SetHeight(first, 0);
            context.Grid.SetHeight(second, 1);
            Assert.That(context.Traversal.CanPassThrough(mover, first, second), Is.True);

            context.Grid.SetHeight(first, 1);
            context.Grid.SetHeight(second, 2);
            Assert.That(context.Traversal.CanPassThrough(mover, first, second), Is.True);

            context.Grid.SetHeight(first, 0);
            context.Grid.SetHeight(second, 2);
            Assert.That(context.Traversal.CanPassThrough(mover, first, second), Is.False);
        }

        [Test]
        public void CanPassThroughRejectsUnwalkableDestination()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var destination = new GridPosition(1, 0);
            context.Grid.SetWalkable(destination, false);

            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(0, 0), destination),
                Is.False);
        }

        [Test]
        public void CanStopAtAllowsOnlyEmptyWalkableInBoundsCell()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var empty = new GridPosition(1, 1);
            var unwalkable = new GridPosition(2, 1);
            context.Grid.SetWalkable(unwalkable, false);

            Assert.That(context.Traversal.CanStopAt(mover, empty), Is.True);
            Assert.That(context.Traversal.CanStopAt(mover, unwalkable), Is.False);
            Assert.That(context.Traversal.CanStopAt(mover, new GridPosition(3, 1)), Is.False);
            Assert.That(context.Traversal.CanStopAt(null, empty), Is.False);
        }

        [Test]
        public void AllyOccupiedDestinationAllowsPassButNotStop()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var ally = CreateUnit(2, UnitTeam.Player);
            var destination = new GridPosition(1, 0);
            Assert.That(context.Registry.Register(ally), Is.True);
            Assert.That(context.Occupancy.TryOccupy(destination, ally.Id), Is.True);

            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(0, 0), destination),
                Is.True);
            Assert.That(context.Traversal.CanStopAt(mover, destination), Is.False);
        }

        [Test]
        public void EnemyOccupiedDestinationBlocksPassAndStop()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var enemy = CreateUnit(2, UnitTeam.Enemy);
            var destination = new GridPosition(1, 0);
            Assert.That(context.Registry.Register(enemy), Is.True);
            Assert.That(context.Occupancy.TryOccupy(destination, enemy.Id), Is.True);

            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(0, 0), destination),
                Is.False);
            Assert.That(context.Traversal.CanStopAt(mover, destination), Is.False);
        }

        [Test]
        public void UnknownOccupiedDestinationBlocksPassAndStop()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var destination = new GridPosition(1, 0);
            Assert.That(context.Occupancy.TryOccupy(destination, new EntityId(99)), Is.True);

            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(0, 0), destination),
                Is.False);
            Assert.That(context.Traversal.CanStopAt(mover, destination), Is.False);
        }

        [Test]
        public void MoverOccupyingDestinationIsRejectedAsInvalidTraversalState()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var destination = new GridPosition(1, 0);
            Assert.That(context.Registry.Register(mover), Is.True);
            Assert.That(context.Occupancy.TryOccupy(destination, mover.Id), Is.True);

            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(0, 0), destination),
                Is.False);
            Assert.That(context.Traversal.CanStopAt(mover, destination), Is.False);
        }

        [Test]
        public void CanPassThroughRejectsNullMoverAndOutOfBoundsPositions()
        {
            var context = CreateContext();
            var mover = CreateUnit(1, UnitTeam.Player);
            var inside = new GridPosition(0, 0);

            Assert.That(
                context.Traversal.CanPassThrough(null, inside, new GridPosition(1, 0)),
                Is.False);
            Assert.That(
                context.Traversal.CanPassThrough(mover, new GridPosition(-1, 0), inside),
                Is.False);
            Assert.That(
                context.Traversal.CanPassThrough(mover, inside, new GridPosition(3, 0)),
                Is.False);
        }

        [Test]
        public void OrthogonalNeighborQueryRemainsSpatialOnly()
        {
            var context = CreateContext();
            var center = new GridPosition(1, 1);
            var blockedNeighbor = new GridPosition(2, 1);
            context.Grid.SetHeight(blockedNeighbor, 2);
            context.Grid.SetWalkable(blockedNeighbor, false);
            Assert.That(
                context.Occupancy.TryOccupy(blockedNeighbor, new EntityId(99)),
                Is.True);

            var neighbors = context.Grid.GetOrthogonalNeighbors(center)
                .Select(cell => cell.Position)
                .ToArray();

            Assert.That(neighbors, Does.Contain(blockedNeighbor));
            Assert.That(neighbors, Has.Length.EqualTo(4));
        }

        private static TraversalContext CreateContext()
        {
            var grid = new GridState(3, 3);
            var occupancy = new GridOccupancy();
            var registry = new UnitRegistry();
            return new TraversalContext(
                grid,
                occupancy,
                registry,
                new GridTraversal(grid, occupancy, registry));
        }

        private static UnitState CreateUnit(int id, UnitTeam team)
        {
            return new UnitState(new EntityId(id), team);
        }

        private sealed class TraversalContext
        {
            public TraversalContext(
                GridState grid,
                GridOccupancy occupancy,
                UnitRegistry registry,
                GridTraversal traversal)
            {
                Grid = grid;
                Occupancy = occupancy;
                Registry = registry;
                Traversal = traversal;
            }

            public GridState Grid { get; }

            public GridOccupancy Occupancy { get; }

            public UnitRegistry Registry { get; }

            public GridTraversal Traversal { get; }
        }
    }
}
