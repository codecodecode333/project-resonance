using System;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectResonance.Presentation
{
    public sealed class IsometricGridPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap terrainTop;
        [SerializeField] private Tilemap terrainSide;
        [SerializeField] private TileBase grassTop;
        [SerializeField] private TileBase grassTopVariation;
        [SerializeField] private TileBase cliffLeft;
        [SerializeField] private TileBase cliffRight;
        [SerializeField] private TileBase cliffBoth;

        public void Clear()
        {
            if (terrainTop != null) terrainTop.ClearAllTiles();
            if (terrainSide != null) terrainSide.ClearAllTiles();
        }

        public void Render(GridState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (terrainTop == null || terrainSide == null || terrainTop == terrainSide
                || grassTop == null || cliffLeft == null || cliffRight == null || cliffBoth == null)
            {
                throw new InvalidOperationException("Assign distinct top/side Tilemaps and the prototype tiles.");
            }

            Clear();
            for (var x = 0; x < state.Width; x++)
            {
                for (var z = 0; z < state.Depth; z++)
                {
                    var position = new GridPosition(x, z);
                    var height = state.GetCell(position).Height;
                    var top = grassTopVariation != null && (x * 3 + z * 7) % 5 == 0
                        ? grassTopVariation : grassTop;
                    terrainTop.SetTile(GridPresentationMapper.ToTilemapCell(position, height), top);

                    // Fixed isometric camera: -X is the visible left face, -Z the right face.
                    // Outside the board is one layer below ground, giving the sample a solid rim.
                    var leftHeight = HeightOrOutside(state, new GridPosition(x - 1, z));
                    var rightHeight = HeightOrOutside(state, new GridPosition(x, z - 1));
                    for (var level = 0; level <= height; level++)
                    {
                        var leftVisible = level > leftHeight;
                        var rightVisible = level > rightHeight;
                        if (!leftVisible && !rightVisible) continue;
                        var side = leftVisible && rightVisible ? cliffBoth
                            : leftVisible ? cliffLeft : cliffRight;
                        terrainSide.SetTile(GridPresentationMapper.ToTilemapCell(position, level), side);
                    }
                }
            }
            terrainTop.CompressBounds();
            terrainSide.CompressBounds();
        }

        private static int HeightOrOutside(GridState state, GridPosition position)
        {
            return state.TryGetCell(position, out var cell) ? cell.Height : -1;
        }
    }
}
