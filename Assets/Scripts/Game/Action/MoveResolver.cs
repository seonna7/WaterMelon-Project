using Game.GamePlay;
using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    namespace Action
    {
        public class MoveResolver
        {
            public MoveResult TryMove(ChessPiece piece, GridManager grid, Vector2Int targetPos)
            {
                if (piece == null)
                    return MoveResult.CreateFail(MoveFailReason.PieceIsNull, Vector2Int.zero, targetPos);

                if (grid == null)
                    return MoveResult.CreateFail(MoveFailReason.InvalidPosition, piece.GridPosition, targetPos);

                if (piece.IsDead)
                    return MoveResult.CreateFail(MoveFailReason.PieceIsDead, piece.GridPosition, targetPos);

                Vector2Int from = piece.GridPosition;

                if (grid.IsInsideGrid(from) == false || grid.IsInsideGrid(targetPos) == false)
                {
                    return MoveResult.CreateFail(MoveFailReason.NotInsideBoard, from, targetPos);
                }

                if (grid.IsWalkable(targetPos) == false)
                {
                    return MoveResult.CreateFail(MoveFailReason.TargetNotWalkable, from, targetPos);
                }

                if (grid.IsEmpty(targetPos) == false)
                {
                    return MoveResult.CreateFail(MoveFailReason.TargetOccupied, from, targetPos);
                }

                List<Vector2Int> possibleMoves = piece.GetPossibleMoves(grid);
                if (ContainsPosition(possibleMoves, targetPos) == false)
                {
                    return MoveResult.CreateFail(MoveFailReason.InvalidMovePattern, from, targetPos);
                }

                bool moved = grid.MovePiece(piece, targetPos);
                if (moved == false)
                {
                    return MoveResult.CreateFail(MoveFailReason.InvalidPosition, from, targetPos);
                }

                return MoveResult.CreateSuccess(from, targetPos);
            }

            private bool ContainsPosition(List<Vector2Int> positions, Vector2Int targetPos)
            {
                if (positions == null)
                    return false;

                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i] == targetPos)
                        return true;
                }

                return false;
            }
        }
    }
}
