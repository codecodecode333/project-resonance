using System;
using NUnit.Framework;
using Riftchord.Core;
using Riftchord.Grid;
using Riftchord.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Riftchord.Tests.EditMode
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
        public void RenderBuildsWholeBlockColumnsWithoutChangingDomainAndClearsStaleTiles()
        {
            var root = new GameObject("TestGrid", typeof(UnityEngine.Grid));
            var blocks = MakeMap(root, "Blocks");
            var presenter = root.AddComponent<IsometricBlockGridPresenter>();
            var grass = ScriptableObject.CreateInstance<Tile>();
            var variation = ScriptableObject.CreateInstance<Tile>();
            try
            {
                Assign(presenter, "terrainBlocks", blocks);
                Assign(presenter, "grassBlock", grass);
                Assign(presenter, "grassBlockVariation", variation);
                var grid = new GridState(3, 3);
                var center = new GridPosition(1, 1);
                grid.SetHeight(center, 2);
                grid.SetHeight(new GridPosition(0, 1), 1);
                grid.SetWalkable(center, false);

                presenter.Render(grid);

                for (var level = 0; level <= 2; level++)
                    Assert.That(blocks.GetTile(new Vector3Int(1, 1, level)), Is.SameAs(variation));
                Assert.That(blocks.HasTile(new Vector3Int(1, 1, 3)), Is.False);
                Assert.That(blocks.GetTile(new Vector3Int(0, 1, 0)), Is.SameAs(grass));
                Assert.That(blocks.GetTile(new Vector3Int(0, 1, 1)), Is.SameAs(grass));
                Assert.That(blocks.HasTile(new Vector3Int(0, 1, 2)), Is.False);
                Assert.That(blocks.GetTile(Vector3Int.zero), Is.SameAs(variation));
                Assert.That(blocks.HasTile(new Vector3Int(0, 0, 1)), Is.False);
                Assert.That(grid.GetCell(center).Height, Is.EqualTo(2));
                Assert.That(grid.GetCell(center).IsWalkable, Is.False);
                Assert.That(grid.GetCell(new GridPosition(0, 1)).Height, Is.EqualTo(1));
                Assert.Throws<ArgumentNullException>(() => presenter.Render(null));
                Assert.That(blocks.GetTile(new Vector3Int(1, 1, 2)), Is.SameAs(variation));
                Assign(presenter, "grassBlock", null);
                Assert.Throws<InvalidOperationException>(() => presenter.Render(grid));
                Assert.That(blocks.GetTile(new Vector3Int(1, 1, 2)), Is.SameAs(variation));
                Assign(presenter, "grassBlock", grass);

                grid.SetHeight(center, 0);
                Assign(presenter, "grassBlockVariation", null);
                presenter.Render(grid);
                Assert.That(blocks.GetTile(new Vector3Int(1, 1, 0)), Is.SameAs(grass));
                Assert.That(blocks.HasTile(new Vector3Int(1, 1, 1)), Is.False);
                Assert.That(blocks.HasTile(new Vector3Int(1, 1, 2)), Is.False);
                presenter.Clear();
                Assert.That(blocks.GetUsedTilesCount(), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(grass);
                Object.DestroyImmediate(variation);
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
