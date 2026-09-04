using Riftchord.Core;
using Riftchord.Grid;
using UnityEngine;

namespace Riftchord.Presentation
{
    public sealed class BattlePrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private IsometricBlockGridPresenter presenter;

        private void Awake()
        {
            RenderDemo();
        }

        [ContextMenu("Render Demo Grid")]
        public void RenderDemo()
        {
            if (presenter == null)
            {
                throw new System.InvalidOperationException("Assign the demo grid presenter.");
            }
            presenter.Render(CreateDemoGrid());
        }

        public static GridState CreateDemoGrid()
        {
            var state = new GridState(10, 8);
            // Rows run from logical Z=0 (front) to Z=7 (back), not screen Y coordinates.
            var heights = new[]
            {
                "0000000000",
                "0000000000",
                "0001111000",
                "0011111200",
                "0011222200",
                "0011222200",
                "0001222200",
                "0000000000",
            };
            for (var z = 0; z < state.Depth; z++)
            {
                for (var x = 0; x < state.Width; x++)
                {
                    state.SetHeight(new GridPosition(x, z), heights[z][x] - '0');
                }
            }
            return state;
        }
    }
}
