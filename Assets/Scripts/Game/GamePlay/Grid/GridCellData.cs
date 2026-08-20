using UnityEngine;

namespace Game.GamePlay.Grid
{
    [System.Serializable]
    public sealed class GridCellData
    {
        public Vector2Int GridPosition { get; }

        public TileType TileType { get; private set; }

        public ChessPiece OccupiedPiece { get; private set; }

        public bool IsOccupied => OccupiedPiece != null;

        public GridCellData(
            Vector2Int gridPosition,
            TileType tileType = TileType.Normal)
        {
            GridPosition = gridPosition;
            TileType = tileType;
        }

        public void SetTileType(TileType tileType)
        {
            TileType = tileType;
        }

        public bool TrySetOccupiedPiece(ChessPiece piece)
        {
            if (piece == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GridCellData)}] " +
                    $"{GridPosition} 셀에 null 말을 배치하려고 했습니다.");

                return false;
            }

            if (IsOccupied)
                return false;

            OccupiedPiece = piece;
            return true;
        }

        public bool TryClearOccupiedPiece(ChessPiece expectedPiece = null)
        {
            if (!IsOccupied)
                return false;

            if (expectedPiece != null && OccupiedPiece != expectedPiece)
                return false;

            OccupiedPiece = null;
            return true;
        }

        public bool ContainsPiece(ChessPiece piece)
        {
            return piece != null && OccupiedPiece == piece;
        }
    }
}
