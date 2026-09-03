using System;
using NUnit.Framework;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using ProjectResonance.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace ProjectResonance.Tests.EditMode
{
    public sealed class GridPresentationTests
    {
        [Test]
        public void MapperKeepsLogicalXZAndHeightOnSeparateTilemapAxes()
        {
            Assert.That(GridPresentationMapper.ToTilemapCell(new GridPosition(3, 5), 2),
                Is.EqualTo(new Vector3Int(3, 5, 2)));
            Assert.That(GridPresentationMapper.ToTilemapCell(new GridPosition(3, 5), 0),
                Is.EqualTo(new Vector3Int(3, 5, 0)));
        }

        [Test]
        public void RenderUsesDomainHeightsExposesOnlyFrontCliffsAndClearsStaleTiles()
        {
            var root = new GameObject("TestGrid", typeof(UnityEngine.Grid));
            var top = MakeMap(root, "Top");
            var side = MakeMap(root, "Side");
            var presenter = root.AddComponent<IsometricGridPresenter>();
            var grass = ScriptableObject.CreateInstance<Tile>();
            var left = ScriptableObject.CreateInstance<Tile>();
            var right = ScriptableObject.CreateInstance<Tile>();
            var both = ScriptableObject.CreateInstance<Tile>();
            try
            {
                Assign(presenter, "terrainTop", top);
                Assign(presenter, "terrainSide", side);
                Assign(presenter, "grassTop", grass);
                Assign(presenter, "cliffLeft", left);
                Assign(presenter, "cliffRight", right);
                Assign(presenter, "cliffBoth", both);
                var grid = new GridState(3, 3);
                var center = new GridPosition(1, 1);
                grid.SetHeight(center, 2);
                grid.SetHeight(new GridPosition(0, 1), 1);
                grid.SetWalkable(center, false);

                presenter.Render(grid);

                Assert.That(top.GetTile(new Vector3Int(1, 1, 2)), Is.SameAs(grass));
                Assert.That(top.HasTile(new Vector3Int(1, 1, 0)), Is.False);
                Assert.That(side.GetTile(new Vector3Int(1, 1, 1)), Is.SameAs(right));
                Assert.That(side.GetTile(new Vector3Int(1, 1, 2)), Is.SameAs(both));
                Assert.That(side.HasTile(new Vector3Int(1, 1, 0)), Is.False);
                Assert.That(side.GetTile(Vector3Int.zero), Is.SameAs(both));
                Assert.That(grid.GetCell(center).Height, Is.EqualTo(2));
                Assert.That(grid.GetCell(center).IsWalkable, Is.False);
                Assert.That(grid.GetCell(new GridPosition(0, 1)).Height, Is.EqualTo(1));
                Assert.Throws<ArgumentNullException>(() => presenter.Render(null));
                Assert.That(top.GetTile(new Vector3Int(1, 1, 2)), Is.SameAs(grass));

                grid.SetHeight(center, 0);
                presenter.Render(grid);
                Assert.That(top.GetTile(new Vector3Int(1, 1, 0)), Is.SameAs(grass));
                Assert.That(top.HasTile(new Vector3Int(1, 1, 2)), Is.False);
                Assert.That(side.HasTile(new Vector3Int(1, 1, 1)), Is.False);
                Assert.That(side.HasTile(new Vector3Int(1, 1, 2)), Is.False);
                presenter.Clear();
                Assert.That(top.GetUsedTilesCount(), Is.Zero);
                Assert.That(side.GetUsedTilesCount(), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(grass);
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
                Object.DestroyImmediate(both);
            }
        }

        private static Tilemap MakeMap(GameObject root, string name)
        {
            var go = new GameObject(name, typeof(Tilemap));
            go.transform.SetParent(root.transform);
            return go.GetComponent<Tilemap>();
        }

        private static void Assign(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
