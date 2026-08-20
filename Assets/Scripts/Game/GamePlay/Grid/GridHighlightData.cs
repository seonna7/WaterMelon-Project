using UnityEngine;

namespace Game.GamePlay.Grid
{
    public readonly struct GridHighlightData
    {
        public Vector2Int GridPosition { get; }

        public GridHighlightType Type { get; }

        public GridHighlightData(
            Vector2Int gridPosition,
            GridHighlightType type)
        {
            GridPosition = gridPosition;
            Type = type;
        }
    }
}