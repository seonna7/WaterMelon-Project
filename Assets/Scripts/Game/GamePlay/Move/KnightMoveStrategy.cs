using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace MoveStrategy
        {
            public class KnightMoveStrategy : IChessMoveStrategy
            {
                private static readonly Vector2Int[] MoveOffsets =
                {
                new Vector2Int( 1,  2),
                new Vector2Int( 2,  1),
                new Vector2Int( 2, -1),
                new Vector2Int( 1, -2),
                new Vector2Int(-1, -2),
                new Vector2Int(-2, -1),
                new Vector2Int(-2,  1),
                new Vector2Int(-1,  2)
            };

                public List<Vector2Int> GetAvailableMoves(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> moves = new List<Vector2Int>();

                    foreach (Vector2Int offset in MoveOffsets)
                    {
                        Vector2Int targetPos = piece.GridPosition + offset;

                        if (gridManager.IsInsideGrid(targetPos) == false)
                            continue;

                        if (gridManager.IsEmpty(targetPos) || gridManager.IsEnemy(targetPos, piece.Color))
                        {
                            moves.Add(targetPos);
                        }
                    }

                    return moves;
                }

                public List<Vector2Int> GetDirectAttackPositions(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> positions = new();

                    foreach (Vector2Int offset in MoveOffsets)
                    {
                        Vector2Int targetPos =
                            piece.GridPosition + offset;

                        if (!gridManager.IsInsideGrid(targetPos))
                            continue;

                        if (gridManager.IsEnemy(targetPos, piece.Color))
                        {
                            positions.Add(targetPos);
                        }
                    }

                    return positions;
                }
            }
        }
    }
}
