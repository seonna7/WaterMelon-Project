using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    public interface IChessMoveStrategy
    {
        List<Vector2Int> GetAvailableMoves(
            ChessPiece piece,
            GridManager gridManager
        );

        List<Vector2Int> GetDirectAttackPositions(
            ChessPiece piece,
            GridManager gridManager
        );
    }
}
