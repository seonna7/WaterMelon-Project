using Game.GamePlay;
using Game.GamePlay.Grid;
using UnityEngine;

namespace Game
{
    namespace Action
    {
        public class ActionCostResolver
        {
            public int GetMoveCost(ChessPiece piece, GridManager grid, Vector2Int targetPos)
            {
                if (piece == null)
                    return 0;

                int moveDistance = GetMoveDistance(piece.GridPosition, targetPos);
                int baseCost = GetBaseMoveCost(piece.movementType, moveDistance);

                int terrainExtraCost = GetTerrainExtraCost(grid, targetPos);

                return baseCost + terrainExtraCost;
            }

            public int GetAttackCost(ChessPiece piece, GridManager grid, Vector2Int targetPos)
            {
                if (piece == null)
                    return 0;

                int baseCost = GetBaseAttackCost(piece);

                int terrainExtraCost = 0;

                // 필요하면 공격 지형 추가 비용 여기서 계산

                return baseCost + terrainExtraCost;
            }

            private int GetBaseMoveCost(MovementType pieceType, int moveDistance)
            {
                switch (pieceType)
                {
                    case MovementType.King:
                        return 1;

                    case MovementType.Pawn:
                        return 2;

                    case MovementType.Knight:
                        return 2;

                    case MovementType.Bishop:
                        return 3;

                    case MovementType.Queen:
                        return GetQueenMoveCost(moveDistance);

                    case MovementType.Rook:
                        return GetRookMoveCost(moveDistance);

                    default:
                        return 1;
                }
            }

            private int GetBaseAttackCost(ChessPiece piece)
            {
                if (piece == null)
                    return 0;

                // 현재 규칙상 공격도 행동 젬 소모로 처리하되,
                // 일단 이동과 동일 비용으로 맞춰도 되고 분리해도 됨.
                // 1차 버전은 piece별 기본값과 동일하게 둠.
                return GetBaseMoveCost(piece.movementType, 1);
            }

            private int GetQueenMoveCost(int moveDistance)
            {
                if (moveDistance <= 2)
                    return 1;

                if (moveDistance <= 4)
                    return 2;

                if (moveDistance <= 6)
                    return 3;

                return 4;
            }

            private int GetRookMoveCost(int moveDistance)
            {
                if (moveDistance <= 3)
                    return 2;

                if (moveDistance <= 6)
                    return 3;

                return 4;
            }

            private int GetTerrainExtraCost(GridManager grid, Vector2Int targetPos)
            {
                if (grid == null)
                    return 0;

                // 나중에 타일 시스템 붙이면 여기서 늪 판정
                // 예: if (board.GetTileType(targetPos) == TileType.Swamp) return 1;
                return 0;
            }

            private int GetMoveDistance(Vector2Int from, Vector2Int to)
            {
                Vector2Int delta = to - from;
                return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            }
        }
    }
}
