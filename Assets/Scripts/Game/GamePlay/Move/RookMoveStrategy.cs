using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.MoveStrategy
{
    public sealed class RookMoveStrategy
        : IChessMoveStrategy
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public List<Vector2Int> GetAvailableMoves(
            ChessPiece piece,
            GridManager gridManager)
        {
            List<Vector2Int> positions = new();

            if (!IsValid(piece, gridManager))
                return positions;

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int position =
                    piece.GridPosition + direction;

                while (gridManager.IsInsideGrid(position))
                {
                    ChessPiece occupant =
                        gridManager.GetPieceAt(position);

                    if (occupant != null)
                        break;

                    positions.Add(position);
                    position += direction;
                }
            }

            return positions;
        }

        public List<Vector2Int> GetDirectAttackPositions(
            ChessPiece piece,
            GridManager gridManager)
        {
            List<Vector2Int> positions = new();

            if (!IsValid(piece, gridManager))
                return positions;

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int position =
                    piece.GridPosition + direction;

                while (gridManager.IsInsideGrid(position))
                {
                    ChessPiece occupant =
                        gridManager.GetPieceAt(position);

                    if (occupant == null)
                    {
                        position += direction;
                        continue;
                    }

                    if (occupant.Color != piece.Color &&
                        !occupant.IsDead)
                    {
                        positions.Add(position);
                    }

                    break;
                }
            }

            return positions;
        }

        private static bool IsValid(
            ChessPiece piece,
            GridManager gridManager)
        {
            return piece != null &&
                   gridManager != null &&
                   piece.IsPlaced &&
                   !piece.IsDead;
        }
    }
}
