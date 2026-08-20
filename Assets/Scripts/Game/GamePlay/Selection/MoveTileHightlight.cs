using UnityEngine;

namespace Game.GamePlay.Selection
{
    public class MoveTileHighlight : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }

        public void Initialize(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
        }
    }
}
