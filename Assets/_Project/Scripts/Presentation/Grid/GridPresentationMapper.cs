using ProjectResonance.Core;
using UnityEngine;

namespace ProjectResonance.Presentation
{
    public static class GridPresentationMapper
    {
        public static Vector3Int ToTilemapCell(GridPosition position, int height)
        {
            return new Vector3Int(position.X, position.Z, height);
        }
    }
}
