using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace MoveStrategy
        {
            public class QueenMoveStrategy : IChessMoveStrategy
            {
                public List<Vector2Int> GetAvailableMoves(
                    ChessPiece piece,
                    GridManager gridManager)
                {
                    List<Vector2Int> moves = new();

                    Vector2Int[] directions =
                    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,

        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int current =
                            piece.GridPosition;

                        while (true)
                        {
                            current += direction;

                            if (!gridManager.IsInsideGrid(current))
                                break;

                            ChessPiece target =
                                gridManager.GetPieceAt(current);

                            // 빈칸
                            if (target == null)
                            {
                                moves.Add(current);
                                continue;
                            }

                            /*
                             * 말을 만나는 순간 이동 탐색 종료.
                             *
                             * 적이라면 GetDirectAttackPositions()가
                             * 별도로 공격 대상으로 반환한다.
                             *
                             * 아군이라면 그냥 막힌다.
                             */
                            break;
                        }
                    }

                    return moves;
                }
                public List<Vector2Int> GetDirectAttackPositions(
                    ChessPiece piece,
                    GridManager gridManager)
                {
                    List<Vector2Int> attackPositions =
                        new List<Vector2Int>();

                    if (piece == null ||
                        gridManager == null)
                    {
                        return attackPositions;
                    }

                    Vector2Int[] directions =
                    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,

        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

                    Vector2Int start =
                        piece.GridPosition;

                    Debug.Log(
                        $"[QUEEN ATTACK] START | " +
                        $"Piece={piece.name} | " +
                        $"Color={piece.Color} | " +
                        $"Start={start}"
                    );

                    foreach (Vector2Int direction
                             in directions)
                    {
                        Vector2Int current =
                            start + direction;

                        while (gridManager.IsInsideGrid(
                                   current))
                        {
                            ChessPiece occupant =
                                gridManager.GetPieceAt(
                                    current
                                );

                            Debug.Log(
                                $"[QUEEN ATTACK] SCAN | " +
                                $"Direction={direction} | " +
                                $"Position={current} | " +
                                $"Occupant=" +
                                $"{(occupant != null ? occupant.name : "EMPTY")} | " +
                                $"Color=" +
                                $"{(occupant != null ? occupant.Color.ToString() : "-")}"
                            );

                            /*
                             * 빈칸이면 계속 탐색.
                             */
                            if (occupant == null)
                            {
                                current += direction;
                                continue;
                            }

                            /*
                             * 말 발견.
                             *
                             * 적이면 직접공격 대상으로 추가.
                             */
                            if (occupant != piece &&
                                !occupant.IsDead &&
                                occupant.Color != piece.Color)
                            {
                                attackPositions.Add(
                                    current
                                );

                                Debug.Log(
                                    $"[QUEEN ATTACK] ★ ENEMY FOUND | " +
                                    $"Target={occupant.name} | " +
                                    $"Position={current}"
                                );
                            }
                            else
                            {
                                Debug.Log(
                                    $"[QUEEN ATTACK] BLOCKED | " +
                                    $"Piece={occupant.name} | " +
                                    $"SameTeam=" +
                                    $"{occupant.Color == piece.Color}"
                                );
                            }

                            /*
                             * 아군이든 적이든 말을 하나 만났으면
                             * 그 방향 탐색 종료.
                             *
                             * 말을 관통해서 뒤쪽 적을 공격하면 안 됨.
                             */
                            break;
                        }
                    }

                    Debug.Log(
                        $"[QUEEN ATTACK] END | " +
                        $"Count={attackPositions.Count}"
                    );

                    return attackPositions;
                }
            }
        }
    }
}

