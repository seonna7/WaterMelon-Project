using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace MoveStrategy
        {
            public class PawnMoveStrategy : IChessMoveStrategy
            {
                public List<Vector2Int> GetAvailableMoves(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> moves = new List<Vector2Int>();

                    int forwardY = 0;

                    if (piece.Color == PieceColor.White)
                    {
                        forwardY = 1;
                    }
                    else
                    {
                        forwardY = -1;
                    }

                    Vector2Int forwardPos = new Vector2Int(
                        piece.GridPosition.x,
                        piece.GridPosition.y + forwardY
                    );

                    if (gridManager.IsInsideGrid(forwardPos) && gridManager.IsEmpty(forwardPos))
                    {
                        moves.Add(forwardPos);
                    }

                    return moves;
                }

                public List<Vector2Int> GetDirectAttackPositions(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> positions = new();

                    int forward =
                        piece.Color == PieceColor.White ? 1 : -1;

                    Vector2Int left =
                        new Vector2Int(
                            piece.GridPosition.x - 1,
                            piece.GridPosition.y + forward);

                    Vector2Int right =
                        new Vector2Int(
                            piece.GridPosition.x + 1,
                            piece.GridPosition.y + forward);

                    if (gridManager.IsInsideGrid(left) &&
                        gridManager.IsEnemy(left, piece.Color))
                    {
                        positions.Add(left);
                    }

                    if (gridManager.IsInsideGrid(right) &&
                        gridManager.IsEnemy(right, piece.Color))
                    {
                        positions.Add(right);
                    }

                    return positions;
                }
            }
        }
    }
}
