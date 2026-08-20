using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace MoveStrategy
        {
            // 왕 움직임은 다르게 해야할듯... 게임이 개 답답해질거같음
            // 범위를 늘리거나, 횟수제로 가능한 움직임을 늘리는 방법도 있을듯
            public class KingMoveStrategy : IChessMoveStrategy
            {
                private static readonly Vector2Int[] MoveOffsets =
                {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right,

                new Vector2Int( 1,  1),
                new Vector2Int( 1, -1),
                new Vector2Int(-1,  1),
                new Vector2Int(-1, -1)
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
