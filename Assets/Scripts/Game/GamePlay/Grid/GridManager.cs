using UnityEngine;

namespace Game.GamePlay.Grid
{
    public class GridManager : MonoBehaviour
    {
        public const int DefaultGridWidth = 10;
        public const int DefaultGridHeight = 10;
        public const float DefaultCellSize = 10f;

        private static readonly Vector2Int InvalidPosition =
            new Vector2Int(-1, -1);

        [Header("Grid")]
        [SerializeField]
        private int gridWidth =
            DefaultGridWidth;

        [SerializeField]
        private int gridHeight =
            DefaultGridHeight;

        [SerializeField]
        private float cellSize =
            DefaultCellSize;

        [SerializeField]
        private bool centerGridOnTransform = true;
        /*
         * GridManager Transform을 기준점으로 사용하고,
         * 이 값은 그 Transform 안에서의 Local Offset이다.
         *
         * 대부분 (0,0,0)으로 두면 된다.
         */
        [SerializeField]
        private Vector3 localOriginOffset =
            Vector3.zero;

        /*
         * 체스말을 셀 중심에 놓을 때
         * Grid 평면에서 얼마만큼 띄울지.
         *
         * 기존 GridToWorld()의 +0.6f를
         * SerializeField로 분리했다.
         */
        [SerializeField]
        private float pieceHeightOffset =
            0.6f;

        [Header("Base Direction")]
        [SerializeField]
        private BaseDirection whiteEnemyBaseDirection =
            BaseDirection.Up;

        [SerializeField]
        private BaseDirection blackEnemyBaseDirection =
            BaseDirection.Down;

        private GridCellData[,] cells;

        public int GridWidth =>
            gridWidth;

        public int GridHeight =>
            gridHeight;

        public float CellSize =>
            cellSize;

        public Vector3 LocalOriginOffset =>
            localOriginOffset;

        /*
         * 다른 기존 코드와의 호환을 위해
         * WorldOrigin 프로퍼티는 유지한다.
         *
         * 이제 Inspector에 저장된 독립 World 좌표가 아니라
         * GridManager Transform 기준 원점을 반환한다.
         */
        public Vector3 WorldOrigin =>
            transform.TransformPoint(
                EffectiveLocalOrigin);

        public bool IsInitialized =>
            cells != null;

        private void Awake()
        {
            InitializeGrid();
        }

        private void OnValidate()
        {
            ValidateSettings();
        }


        public Vector3 EffectiveLocalOrigin
        {
            get
            {
                if (!centerGridOnTransform)
                    return localOriginOffset;

                return localOriginOffset +
                       new Vector3(
                           -gridWidth * cellSize * 0.5f,
                           0f,
                           -gridHeight * cellSize * 0.5f);
            }
        }

        public void InitializeGrid()
        {
            ValidateSettings();

            cells =
                new GridCellData[
                    gridWidth,
                    gridHeight
                ];

            for (int x = 0;
                 x < gridWidth;
                 x++)
            {
                for (int y = 0;
                     y < gridHeight;
                     y++)
                {
                    Vector2Int gridPosition =
                        new Vector2Int(
                            x,
                            y
                        );

                    cells[x, y] =
                        new GridCellData(
                            gridPosition,
                            TileType.Normal
                        );
                }
            }
        }

        private void ValidateSettings()
        {
            if (gridWidth <= 0)
            {
                gridWidth =
                    DefaultGridWidth;
            }

            if (gridHeight <= 0)
            {
                gridHeight =
                    DefaultGridHeight;
            }

            if (cellSize <= 0f)
            {
                cellSize =
                    DefaultCellSize;
            }
        }

        #region Cell Query

        public bool IsInsideGrid(
            Vector2Int position)
        {
            return
                position.x >= 0 &&
                position.x < gridWidth &&
                position.y >= 0 &&
                position.y < gridHeight;
        }

        public bool IsOutsideGrid(
            Vector2Int position)
        {
            return !IsInsideGrid(
                position
            );
        }

        public GridCellData GetCellAt(
            Vector2Int position)
        {
            if (!IsInitialized)
                return null;

            if (!IsInsideGrid(
                    position))
            {
                return null;
            }

            return cells[
                position.x,
                position.y
            ];
        }

        public bool TryGetCell(
            Vector2Int position,
            out GridCellData cell)
        {
            cell =
                GetCellAt(
                    position
                );

            return cell != null;
        }

        public ChessPiece GetPieceAt(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            return cell?
                .OccupiedPiece;
        }

        public TileType GetTileTypeAt(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            return cell != null
                ? cell.TileType
                : TileType.Normal;
        }

        public bool SetTileTypeAt(
            Vector2Int position,
            TileType tileType)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return false;

            cell.SetTileType(
                tileType
            );

            return true;
        }

        #endregion

        #region Cell State

        public bool IsEmpty(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            return cell != null &&
                   !cell.IsOccupied;
        }

        public bool HasPiece(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            return cell != null &&
                   cell.IsOccupied;
        }

        public bool IsEnemy(
            Vector2Int position,
            PieceColor myColor)
        {
            ChessPiece target =
                GetPieceAt(
                    position
                );

            return target != null &&
                   target.Color != myColor;
        }

        public bool IsAlly(
            Vector2Int position,
            PieceColor myColor)
        {
            ChessPiece target =
                GetPieceAt(
                    position
                );

            return target != null &&
                   target.Color == myColor;
        }

        #endregion

        #region Tile Rules

        public bool IsWalkable(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return false;

            return IsTileWalkable(
                cell.TileType
            );
        }

        public bool BlocksVision(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return false;

            return DoesTileBlockVision(
                cell.TileType
            );
        }

        public int GetExtraMoveCost(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return 0;

            return GetTileExtraMoveCost(
                cell.TileType
            );
        }

        private static bool IsTileWalkable(
            TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Obstacle:
                case TileType.BlockedZone:
                    return false;

                default:
                    return true;
            }
        }

        private static bool DoesTileBlockVision(
            TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Bush:
                case TileType.Obstacle:
                    return true;

                default:
                    return false;
            }
        }

        private static int GetTileExtraMoveCost(
            TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Swamp:
                    return 1;

                default:
                    return 0;
            }
        }

        public bool IsShrinkDamageZone(
            Vector2Int position)
        {
            return GetTileTypeAt(
                       position) ==
                   TileType.ShrinkDamageZone;
        }

        public bool IsBlockedZone(
            Vector2Int position)
        {
            return GetTileTypeAt(
                       position) ==
                   TileType.BlockedZone;
        }

        public bool IsNeutralKingMoveZone(
            Vector2Int position)
        {
            return GetTileTypeAt(
                       position) ==
                   TileType.NeutralKingMoveZone;
        }

        #endregion

        #region Piece Placement

        public bool CanPlacePiece(
            Vector2Int position)
        {
            return
                IsInsideGrid(position) &&
                IsWalkable(position) &&
                IsEmpty(position);
        }

        public bool PlacePiece(
            ChessPiece piece,
            Vector2Int position)
        {
            if (piece == null)
                return false;

            if (!CanPlacePiece(
                    position))
            {
                return false;
            }

            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return false;

            if (!cell.TrySetOccupiedPiece(
                    piece))
            {
                return false;
            }

            piece.SetGridPosition(
                position
            );

            return true;
        }

        public bool RemovePieceAt(
            Vector2Int position)
        {
            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null ||
                !cell.IsOccupied)
            {
                return false;
            }

            ChessPiece piece =
                cell.OccupiedPiece;

            if (!cell.TryClearOccupiedPiece(
                    piece))
            {
                return false;
            }

            piece.SetGridPosition(
                InvalidPosition
            );

            return true;
        }

        public bool RemovePiece(
            ChessPiece piece)
        {
            if (piece == null)
                return false;

            Vector2Int position =
                piece.GridPosition;

            GridCellData cell =
                GetCellAt(
                    position
                );

            if (cell == null)
                return false;

            if (!cell.ContainsPiece(
                    piece))
            {
                return false;
            }

            if (!cell.TryClearOccupiedPiece(
                    piece))
            {
                return false;
            }

            piece.SetGridPosition(
                InvalidPosition
            );

            return true;
        }

        #endregion

        #region Piece Movement

        public bool MovePiece(
            Vector2Int from,
            Vector2Int to)
        {
            if (from == to)
                return false;

            GridCellData fromCell =
                GetCellAt(
                    from
                );

            GridCellData toCell =
                GetCellAt(
                    to
                );

            if (fromCell == null ||
                toCell == null)
            {
                return false;
            }

            ChessPiece movingPiece =
                fromCell.OccupiedPiece;

            if (movingPiece == null)
                return false;

            if (!IsWalkable(to) ||
                toCell.IsOccupied)
            {
                return false;
            }

            /*
             * 목적지를 먼저 점유한 뒤
             * 출발지를 비운다.
             */
            if (!toCell.TrySetOccupiedPiece(
                    movingPiece))
            {
                return false;
            }

            if (!fromCell.TryClearOccupiedPiece(
                    movingPiece))
            {
                toCell.TryClearOccupiedPiece(
                    movingPiece
                );

                return false;
            }

            movingPiece.SetGridPosition(
                to
            );

            Vector3 targetWorld =
                GridToWorld(
                    to
                );

            /*
             * 기존 Piece의 세로 높이를
             * 유지하고 싶다면 이 부분을 유지.
             */
            targetWorld.y =
                movingPiece
                    .transform
                    .position
                    .y;

            movingPiece.transform.position =
                targetWorld;

            return true;
        }

        public bool MovePiece(
            ChessPiece piece,
            Vector2Int targetPosition)
        {
            if (piece == null)
                return false;

            if (!IsInsideGrid(
                    targetPosition))
            {
                return false;
            }

            Vector2Int fromPosition =
                piece.GridPosition;

            GridCellData fromCell =
                GetCellAt(
                    fromPosition
                );

            GridCellData targetCell =
                GetCellAt(
                    targetPosition
                );

            if (fromCell == null ||
                targetCell == null)
            {
                return false;
            }

            if (!fromCell.ContainsPiece(
                    piece))
            {
                Debug.LogWarning(
                    $"[GridManager] " +
                    $"출발 셀 {fromPosition}에 " +
                    $"{piece.name}이 없습니다."
                );

                return false;
            }

            if (targetCell.IsOccupied)
            {
                Debug.LogWarning(
                    $"[GridManager] " +
                    $"도착 셀 {targetPosition}이 " +
                    $"이미 점유되어 있습니다."
                );

                return false;
            }

            if (!IsWalkable(
                    targetPosition))
            {
                return false;
            }

            bool cleared =
                fromCell.TryClearOccupiedPiece(
                    piece
                );

            if (!cleared)
            {
                Debug.LogWarning(
                    $"[GridManager] " +
                    $"출발 셀 정리 실패: " +
                    $"{fromPosition}"
                );

                return false;
            }

            bool occupied =
                targetCell.TrySetOccupiedPiece(
                    piece
                );

            if (!occupied)
            {
                fromCell.TrySetOccupiedPiece(
                    piece
                );

                Debug.LogWarning(
                    $"[GridManager] " +
                    $"도착 셀 점유 실패: " +
                    $"{targetPosition}"
                );

                return false;
            }

            piece.SetGridPosition(
                targetPosition
            );

            Vector3 targetWorldPosition =
                GridToWorld(
                    targetPosition
                );

            targetWorldPosition.y =
                piece.transform.position.y;

            piece.MoveToWorldPosition(
                targetWorldPosition
            );

            return true;
        }

        public bool SwapPieces(
            ChessPiece first,
            ChessPiece second)
        {
            if (first == null ||
                second == null)
            {
                return false;
            }

            if (first == second)
                return false;

            Vector2Int firstPosition =
                first.GridPosition;

            Vector2Int secondPosition =
                second.GridPosition;

            GridCellData firstCell =
                GetCellAt(
                    firstPosition
                );

            GridCellData secondCell =
                GetCellAt(
                    secondPosition
                );

            if (firstCell == null ||
                secondCell == null)
            {
                return false;
            }

            if (!firstCell.ContainsPiece(
                    first) ||
                !secondCell.ContainsPiece(
                    second))
            {
                return false;
            }

            if (!firstCell.TryClearOccupiedPiece(
                    first))
            {
                return false;
            }

            if (!secondCell.TryClearOccupiedPiece(
                    second))
            {
                firstCell.TrySetOccupiedPiece(
                    first
                );

                return false;
            }

            bool placedSecond =
                firstCell.TrySetOccupiedPiece(
                    second
                );

            bool placedFirst =
                secondCell.TrySetOccupiedPiece(
                    first
                );

            if (!placedSecond ||
                !placedFirst)
            {
                firstCell.TryClearOccupiedPiece(
                    second
                );

                secondCell.TryClearOccupiedPiece(
                    first
                );

                firstCell.TrySetOccupiedPiece(
                    first
                );

                secondCell.TrySetOccupiedPiece(
                    second
                );

                return false;
            }

            first.SetGridPosition(
                secondPosition
            );

            second.SetGridPosition(
                firstPosition
            );

            Vector3 firstWorld =
                GridToWorld(
                    secondPosition
                );

            Vector3 secondWorld =
                GridToWorld(
                    firstPosition
                );

            firstWorld.y =
                first.transform.position.y;

            secondWorld.y =
                second.transform.position.y;

            first.transform.position =
                firstWorld;

            second.transform.position =
                secondWorld;

            return true;
        }

        #endregion

        #region Push

        public BaseDirection
            GetEnemyBaseDirection(
                PieceColor targetColor)
        {
            return
                targetColor ==
                PieceColor.White
                    ? whiteEnemyBaseDirection
                    : blackEnemyBaseDirection;
        }

        public Vector2Int
            GetKnockbackDirection(
                PieceColor targetColor)
        {
            return GetEnemyBaseDirection(
                    targetColor)
                .ToVector2Int();
        }

        public Vector2Int
            GetPushDestination(
                Vector2Int startPosition,
                PieceColor targetColor,
                int pushDistance)
        {
            if (pushDistance <= 0)
                return startPosition;

            Vector2Int direction =
                GetKnockbackDirection(
                    targetColor
                );

            return
                startPosition +
                direction * pushDistance;
        }

        public bool TryPushPiece(
            ChessPiece piece,
            int pushDistance)
        {
            if (piece == null ||
                pushDistance <= 0)
            {
                return false;
            }

            Vector2Int currentPosition =
                piece.GridPosition;

            if (!IsInsideGrid(
                    currentPosition))
            {
                return false;
            }

            if (GetPieceAt(
                    currentPosition) !=
                piece)
            {
                return false;
            }

            Vector2Int direction =
                GetKnockbackDirection(
                    piece.Color
                );

            bool movedOrRemoved =
                false;

            for (int i = 0;
                 i < pushDistance;
                 i++)
            {
                Vector2Int nextPosition =
                    currentPosition +
                    direction;

                if (IsOutsideGrid(
                        nextPosition))
                {
                    return RemovePiece(
                        piece
                    );
                }

                if (!IsWalkable(
                        nextPosition))
                {
                    break;
                }

                if (!IsEmpty(
                        nextPosition))
                {
                    break;
                }

                if (!MovePiece(
                        currentPosition,
                        nextPosition))
                {
                    break;
                }

                currentPosition =
                    nextPosition;

                movedOrRemoved =
                    true;
            }

            return movedOrRemoved;
        }

        #endregion

        #region Reset

        public void ClearAllPieces()
        {
            if (!IsInitialized)
                return;

            for (int x = 0;
                 x < gridWidth;
                 x++)
            {
                for (int y = 0;
                     y < gridHeight;
                     y++)
                {
                    GridCellData cell =
                        cells[x, y];

                    if (cell == null ||
                        !cell.IsOccupied)
                    {
                        continue;
                    }

                    ChessPiece piece =
                        cell.OccupiedPiece;

                    if (cell
                        .TryClearOccupiedPiece(
                            piece))
                    {
                        piece.SetGridPosition(
                            InvalidPosition
                        );
                    }
                }
            }
        }

        public void ResetAllTiles(
            TileType defaultTileType =
                TileType.Normal)
        {
            if (!IsInitialized)
                return;

            for (int x = 0;
                 x < gridWidth;
                 x++)
            {
                for (int y = 0;
                     y < gridHeight;
                     y++)
                {
                    cells[x, y]?
                        .SetTileType(
                            defaultTileType
                        );
                }
            }
        }

        #endregion

        #region Coordinate Conversion

        /*
         * GridManager Transform을 기준으로 하는
         * Local Grid 좌표를 World 좌표로 변환한다.
         *
         * GridRenderer에서도 이 함수를 사용한다.
         */
        public Vector3 GridLocalToWorld(
            Vector3 localGridPosition)
        {
            Vector3 localPosition =
                EffectiveLocalOrigin +
                localGridPosition;

            return transform.TransformPoint(
                localPosition);
        }

        /*
         * World → Grid Local 공간.
         */
        public Vector3 WorldToGridLocal(
            Vector3 worldPosition)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(
                    worldPosition);

            return localPosition -
                   EffectiveLocalOrigin;
        }

        /*
         * 셀 중심 World Position.
         */
        public Vector3 GridToWorld(
            Vector2Int gridPosition,
            bool center = true)
        {
            float x =
                gridPosition.x *
                cellSize;

            float z =
                gridPosition.y *
                cellSize;

            if (center)
            {
                x +=
                    cellSize * 0.5f;

                z +=
                    cellSize * 0.5f;
            }

            Vector3 localPosition =
                new Vector3(
                    x,
                    pieceHeightOffset,
                    z
                );

            return GridLocalToWorld(
                localPosition
            );
        }

        /*
         * 셀 평면상의 특정 높이를 사용하는 변환.
         *
         * GridRenderer / Editor Snap 등에 사용한다.
         */
        public Vector3 GridToWorld(
            Vector2Int gridPosition,
            float localHeight,
            bool center)
        {
            float x =
                gridPosition.x *
                cellSize;

            float z =
                gridPosition.y *
                cellSize;

            if (center)
            {
                x +=
                    cellSize * 0.5f;

                z +=
                    cellSize * 0.5f;
            }

            return GridLocalToWorld(
                new Vector3(
                    x,
                    localHeight,
                    z
                )
            );
        }

        public Vector2Int WorldToGrid(
            Vector3 worldPosition)
        {
            Vector3 localPosition =
                WorldToGridLocal(
                    worldPosition
                );

            int x =
                Mathf.FloorToInt(
                    localPosition.x /
                    cellSize
                );

            int y =
                Mathf.FloorToInt(
                    localPosition.z /
                    cellSize
                );

            return new Vector2Int(
                x,
                y
            );
        }

        public bool TryWorldToGrid(
            Vector3 worldPosition,
            out Vector2Int gridPosition)
        {
            gridPosition =
                WorldToGrid(
                    worldPosition
                );

            return IsInsideGrid(
                gridPosition
            );
        }

        /*
         * Edit Mode Grid Snap에서 사용할 수 있는
         * 가장 가까운 셀 중심 계산.
         */
        public bool TryGetNearestCellCenter(
            Vector3 worldPosition,
            out Vector2Int gridPosition,
            out Vector3 worldCenter)
        {
            gridPosition =
                WorldToGrid(
                    worldPosition
                );

            if (!IsInsideGrid(
                    gridPosition))
            {
                worldCenter =
                    worldPosition;

                return false;
            }

            worldCenter =
                GridToWorld(
                    gridPosition
                );

            return true;
        }

        #endregion
    }
}