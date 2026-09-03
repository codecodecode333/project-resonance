using System;
using ProjectResonance.Core;
using ProjectResonance.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectResonance.Presentation
{
    public sealed class IsometricBlockGridPresenter : MonoBehaviour
    {
        [SerializeField] private Tilemap terrainBlocks;
        [SerializeField] private TileBase grassBlock;
        [SerializeField] private TileBase grassBlockVariation;

        public void Clear()
        {
            if (terrainBlocks != null) terrainBlocks.ClearAllTiles();
        }

        public void Render(GridState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (terrainBlocks == null || grassBlock == null)
            {
                throw new InvalidOperationException("Assign the terrain block Tilemap and base block tile.");
            }

            Clear();
            for (var x = 0; x < state.Width; x++)
            {
                for (var z = 0; z < state.Depth; z++)
                {
                    var position = new GridPosition(x, z);
                    var height = state.GetCell(position).Height;
                    var block = grassBlockVariation != null && (x * 3 + z * 7) % 5 == 0
                        ? grassBlockVariation : grassBlock;

                    // A complete sprite per layer fills even an isolated two-step column.
                    // Lower layers are visual support only, not additional logical cells.
                    for (var level = 0; level <= height; level++)
                    {
                        terrainBlocks.SetTile(GridPresentationMapper.ToTilemapCell(position, level), block);
                    }
                }
            }
            terrainBlocks.CompressBounds();
        }
    }
}
