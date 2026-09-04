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
    public sealed class PathFinderTests
    {
        private GridState _grid;
        private GridOccupancy _occupancy;
        private UnitRegistry _registry;
        private UnitPlacementService _placement;
        private GridTraversal _traversal;
        private PathFinder _finder;

        [Test]
        public void InvalidQueryInputsAreRejectedEvenForZeroStepQueries()
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var unplaced = new UnitState(new EntityId(2), UnitTeam.Player);

            Assert.Throws<ArgumentNullException>(() =>
                _finder.TryFindPath(null, mover.Position, 0, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _finder.TryFindPath(mover, mover.Position, -1, out _));
            Assert.Throws<InvalidOperationException>(() =>
                _finder.TryFindPath(unplaced, mover.Position, 0, out _));

            var outside = new UnitState(new EntityId(3), UnitTeam.Player);
            var otherPlacement = new UnitPlacementService(new GridState(5, 1), new GridOccupancy());
            Assert.That(otherPlacement.TryPlace(outside, new GridPosition(4, 0)), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                _finder.TryFindPath(outside, outside.Position, 0, out _));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void StartEqualsTargetSucceedsWithEmptyPathDespiteSelfOccupancy(int maxDistance)
        {
            CreateGrid(2, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            Assert.That(_traversal.CanStopAt(mover, mover.Position), Is.False);

            Assert.That(_finder.TryFindPath(mover, mover.Position, maxDistance, out var path), Is.True);
            Assert.That(path, Is.Empty);
        }

        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(3, 4)]
        public void StraightPathExcludesStartAndIncludesOrderedStepsAndTarget(int targetX, int maxDistance)
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(targetX, 0);
            _grid.SetHeight(new GridPosition(1, 0), 1);

            Assert.That(_finder.TryFindPath(mover, target, maxDistance, out var path), Is.True);
            Assert.That(path, Is.EqualTo(Enumerable.Range(1, targetX)
                .Select(x => new GridPosition(x, 0)).ToArray()));
            AssertValidPath(mover, target, maxDistance, path);
        }

        [Test]
        public void NoRouteQueriesReturnFalseAndAnEmptyPath()
        {
            CreateGrid(4, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(3, 0);
            Assert.That(_finder.TryFindPath(mover, target, 3, out var path), Is.True);

            Assert.That(_finder.TryFindPath(mover, target, 0, out path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, target, 2, out path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, new GridPosition(-1, 0), 4, out path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, new GridPosition(4, 0), 4, out path), Is.False);
            Assert.That(path, Is.Empty);

            _grid.SetWalkable(target, false);
            Assert.That(_finder.TryFindPath(mover, target, 3, out path), Is.False);
            Assert.That(path, Is.Empty);
            _grid.SetWalkable(target, true);
            _grid.SetWalkable(new GridPosition(1, 0), false);
            Assert.That(_finder.TryFindPath(mover, target, int.MaxValue, out path), Is.False);
            Assert.That(path, Is.Empty);
        }

        [TestCase(UnitTeam.Player)]
        [TestCase(UnitTeam.Enemy)]
        public void UnitOccupiedTargetIsRejected(UnitTeam team)
        {
            CreateGrid(2, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(1, 0);
            PlaceUnit(2, team, target);

            Assert.That(_finder.TryFindPath(mover, target, 1, out var path), Is.False);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void AllyCanBeAnIntermediateStepButNotAReachableTarget()
        {
            CreateGrid(3, 1);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var allyPosition = new GridPosition(1, 0);
            var target = new GridPosition(2, 0);
            PlaceUnit(2, UnitTeam.Player, allyPosition);

            var reachable = new ReachabilityFinder(_grid, _traversal).FindReachableCells(mover, 2);
            Assert.That(reachable, Does.Contain(target));
            Assert.That(reachable.Contains(allyPosition), Is.False);
            Assert.That(_finder.TryFindPath(mover, target, 2, out var path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { allyPosition, target }));
            AssertValidPath(mover, target, 2, path);
            Assert.That(_finder.TryFindPath(mover, allyPosition, 2, out path), Is.False);
            Assert.That(path, Is.Empty);
        }

        [TestCase(true, TestName = "EnemyBlocksDirectRouteButAllowsDetour")]
        [TestCase(false, TestName = "UnknownEntityBlocksDirectRouteButAllowsDetour")]
        public void BlockingEntityCannotBeTargetOrIntermediateStep(bool knownEnemy)
        {
            CreateGrid(3, 2);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var barrier = new GridPosition(1, 0);
            var target = new GridPosition(2, 0);
            if (knownEnemy)
            {
                PlaceUnit(2, UnitTeam.Enemy, barrier);
            }
            else
            {
                Assert.That(_occupancy.TryOccupy(barrier, new EntityId(99)), Is.True);
            }

            var detourEntry = new GridPosition(0, 1);
            _grid.SetWalkable(detourEntry, false);
            Assert.That(_finder.TryFindPath(mover, barrier, 4, out var path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, target, 4, out path), Is.False);
            Assert.That(path, Is.Empty);

            _grid.SetWalkable(detourEntry, true);
            Assert.That(_finder.TryFindPath(mover, target, 3, out path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, target, 4, out path), Is.True);
            Assert.That(path, Is.EqualTo(new[]
            {
                detourEntry, new GridPosition(1, 1), new GridPosition(2, 1), target,
            }));
            AssertValidPath(mover, target, 4, path);
        }

        [Test]
        public void HeightBlockedDirectEdgeCanBeReachedByAValidShortestDetour()
        {
            CreateGrid(2, 2);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(0, 0));
            var target = new GridPosition(1, 0);
            var firstStep = new GridPosition(0, 1);
            var secondStep = new GridPosition(1, 1);
            _grid.SetHeight(target, 2);

            Assert.That(_finder.TryFindPath(mover, target, 3, out var path), Is.False);
            Assert.That(path, Is.Empty);
            _grid.SetHeight(firstStep, 1);
            _grid.SetHeight(secondStep, 2);
            Assert.That(_finder.TryFindPath(mover, target, 2, out path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(_finder.TryFindPath(mover, target, 3, out path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { firstStep, secondStep, target }));
            AssertValidPath(mover, target, 3, path);
        }

        [Test]
        public void ReachableTargetsHaveShortestValidPathsWithoutSpecifyingTieOrder()
        {
            CreateGrid(3, 3);
            var mover = PlaceUnit(1, UnitTeam.Player, new GridPosition(1, 1));
            var reachable = new ReachabilityFinder(_grid, _traversal).FindReachableCells(mover, 2);
            Assert.That(reachable.Count, Is.EqualTo(8));

            foreach (var target in reachable)
            {
                Assert.That(_finder.TryFindPath(mover, target, 2, out var path), Is.True);
                var shortestLength = Math.Abs(target.X - mover.Position.X)
                    + Math.Abs(target.Z - mover.Position.Z);
                Assert.That(path.Count, Is.EqualTo(shortestLength));
                AssertValidPath(mover, target, 2, path);
            }
        }

        [Test]
        public void SuccessfulAndFailedQueriesPreserveAllGameState()
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

            var cells = Enumerable.Range(0, _grid.Width)
                .SelectMany(x => Enumerable.Range(0, _grid.Depth).Select(z => _grid.GetCell(new GridPosition(x, z))))
                .ToArray();
            var terrainBefore = cells.Select(cell => (cell.Position, cell.Height, cell.IsWalkable)).ToArray();
            var occupancyBefore = cells.Select(cell =>
                _occupancy.TryGetOccupant(cell.Position, out var id) ? id : default).ToArray();
            var units = new[] { mover, ally, enemy };
            var positionsBefore = units.Select(unit => unit.Position).ToArray();

            var target = new GridPosition(2, 0);
            Assert.That(_finder.TryFindPath(mover, target, 2, out var path), Is.True);
            AssertValidPath(mover, target, 2, path);
            Assert.That(_finder.TryFindPath(mover, enemy.Position, 4, out path), Is.False);
            Assert.That(path, Is.Empty);

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
            _traversal = new GridTraversal(_grid, _occupancy, _registry);
            _finder = new PathFinder(_grid, _traversal);
        }

        private UnitState PlaceUnit(int id, UnitTeam team, GridPosition position)
        {
            var unit = new UnitState(new EntityId(id), team);
            Assert.That(_registry.Register(unit), Is.True);
            Assert.That(_placement.TryPlace(unit, position), Is.True);
            return unit;
        }

        private void AssertValidPath(
            UnitState mover,
            GridPosition target,
            int maxDistance,
            IReadOnlyList<GridPosition> path)
        {
            Assert.That(path.Count, Is.LessThanOrEqualTo(maxDistance));
            Assert.That(path.Contains(mover.Position), Is.False);
            var current = mover.Position;
            foreach (var step in path)
            {
                Assert.That(_traversal.CanPassThrough(mover, current, step), Is.True);
                current = step;
            }

            Assert.That(current, Is.EqualTo(target));
            if (path.Count > 0)
            {
                Assert.That(_traversal.CanStopAt(mover, target), Is.True);
            }
        }
    }
}
