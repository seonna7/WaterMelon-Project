using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace MoveStrategy
        {
            public class BishopMoveStrategy : IChessMoveStrategy
            {
                public List<Vector2Int> GetAvailableMoves(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> moves = new List<Vector2Int>();

                    Vector2Int[] directions =
                    {
                        new Vector2Int(1, 1),
                        new Vector2Int(1, -1),
                        new Vector2Int(-1, 1),
                        new Vector2Int(-1, -1)
                    };

                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int currentPosition = piece.GridPosition;

                        while (true)
                        {
                            currentPosition += direction;

                            GridCellData cell =
                                gridManager.GetCellAt(currentPosition);

                            if (cell == null)
                                break;

                            //if (!gridManager.IsTileWalkable(cell.TileType))
                            //    break;

                            if (cell.IsOccupied)
                                break;

                            moves.Add(currentPosition);
                        }
                    }

                    return moves;
                }

                public List<Vector2Int> GetDirectAttackPositions(ChessPiece piece, GridManager gridManager)
                {
                    List<Vector2Int> positions = new();

                    Vector2Int[] directions =
                    {
        new Vector2Int(1,1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,1),
        new Vector2Int(-1,-1)
    };

                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int current = piece.GridPosition;

                        while (true)
                        {
                            current += direction;

                            if (!gridManager.IsInsideGrid(current))
                                break;

                            ChessPiece target =
                                gridManager.GetPieceAt(current);

                            if (target == null)
                                continue;

                            if (target.Color != piece.Color &&
                                !target.IsDead)
                            {
                                positions.Add(current);
                            }

                            break;
                        }
                    }

                    return positions;
                }
            }
        }
    }
}
