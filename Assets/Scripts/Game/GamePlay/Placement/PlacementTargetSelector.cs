using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.GamePlay.Placement
{
    public sealed class PlacementTargetSelector
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private BattlePlacementController
            placementController;

        [SerializeField]
        private PlacementHighlightManager
            placementHighlightManager;

        [Header("Raycast")]
        [SerializeField]
        private LayerMask boardLayerMask;

        [SerializeField]
        private float rayDistance = 100f;

        [Header("Placement Rule")]
        [Tooltip(
            "White가 배치 가능한 Y 최대값. " +
            "예: 1이면 y=0, 1 영역에 배치할 수 있습니다."
        )]
        [SerializeField]
        [Min(0)]
        private int whitePlacementMaxY = 1;

        [Tooltip(
            "Black이 수동 배치될 경우 사용할 " +
            "보드 위쪽 배치 영역의 깊이입니다."
        )]
        [SerializeField]
        [Min(1)]
        private int blackPlacementDepth = 2;

        private readonly List<Vector2Int>
            validPositions = new();

        private readonly List<Vector2Int>
            invalidPositions = new();

        private bool isShowingHighlights;

        /*
         * 현재 하이라이트가 어느 말을 기준으로
         * 만들어졌는지 기억한다.
         *
         * 새로운 말을 배치하기 시작했는데
         * 이전 하이라이트가 남아 있는 문제를 방지한다.
         */
        private ChessPiece highlightedPiece;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera =
                    Camera.main;
            }

            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (placementController == null)
            {
                placementController =
                    FindFirstObjectByType<
                        BattlePlacementController>();
            }

            if (placementHighlightManager == null)
            {
                placementHighlightManager =
                    FindFirstObjectByType<
                        PlacementHighlightManager>();
            }
        }

        private void Update()
        {
            /*
             * PlacementController가 없으면
             * 아무것도 처리하지 않는다.
             */
            if (placementController == null)
            {
                return;
            }

            /*
             * 현재 배치 모드가 아니라면
             * 남아 있는 하이라이트를 제거한다.
             */
            if (!placementController
                    .IsPlacementMode)
            {
                if (isShowingHighlights)
                {
                    ClearHighlights();
                }

                return;
            }

            ChessPiece currentPiece =
                placementController
                    .CurrentPlacementPiece;

            /*
             * 배치 모드인데 실제 선택된 말이 없다면
             * 비정상 상태이므로 하이라이트를 정리한다.
             */
            if (currentPiece == null)
            {
                ClearHighlights();
                return;
            }

            /*
             * 새로운 말을 배치하기 시작했거나
             * 아직 하이라이트가 만들어지지 않았다면
             * 배치 영역을 다시 계산한다.
             */
            if (!isShowingHighlights ||
                highlightedPiece != currentPiece)
            {
                ShowPlacementHighlights(
                    currentPiece
                );
            }

            /*
             * 우클릭
             * → 현재 배치 선택 취소
             */
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            /*
             * 왼쪽 클릭이 아니면 종료.
             */
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            /*
             * UI 버튼 위를 클릭했을 경우
             * 보드 클릭으로 처리하지 않는다.
             */
            if (EventSystem.current != null &&
                EventSystem.current
                    .IsPointerOverGameObject())
            {
                return;
            }

            TrySelectPlacementTile();
        }

        /*
         * 현재 선택한 말이 배치 가능한 영역을 계산하고
         * 하이라이트를 표시한다.
         */
        private void ShowPlacementHighlights(
            ChessPiece piece)
        {
            ClearHighlights();

            if (piece == null ||
                gridManager == null ||
                placementHighlightManager == null)
            {
                return;
            }

            for (int x = 0;
                 x < gridManager.GridWidth;
                 x++)
            {
                for (int y = 0;
                     y < gridManager.GridHeight;
                     y++)
                {
                    Vector2Int position =
                        new Vector2Int(
                            x,
                            y
                        );

                    /*
                     * 해당 팀의 배치 영역이 아니면
                     * 아무것도 표시하지 않는다.
                     */
                    if (!IsInsidePlacementArea(
                            piece,
                            position))
                    {
                        continue;
                    }

                    /*
                     * 실제로 배치 가능한 칸.
                     */
                    if (gridManager.CanPlacePiece(
                            position))
                    {
                        validPositions.Add(
                            position
                        );
                    }
                    else
                    {
                        /*
                         * 팀의 배치 영역이기는 하지만
                         * 다른 말이나 장애물 등의 이유로
                         * 배치할 수 없는 칸.
                         */
                        invalidPositions.Add(
                            position
                        );
                    }
                }
            }

            placementHighlightManager
                .ShowHighlights(
                    validPositions,
                    invalidPositions
                );

            highlightedPiece = piece;
            isShowingHighlights = true;

            Debug.Log(
                $"[PlacementTargetSelector] " +
                $"배치 영역 표시 | " +
                $"Piece={piece.name} | " +
                $"Team={piece.Color} | " +
                $"Valid={validPositions.Count} | " +
                $"Invalid={invalidPositions.Count}"
            );
        }

        /*
         * 해당 위치가 현재 말의
         * 기본 배치 영역 안인지 확인한다.
         *
         * 여기서는 '영역'만 검사하고
         * 실제로 비어있는지는 검사하지 않는다.
         */
        private bool IsInsidePlacementArea(
            ChessPiece piece,
            Vector2Int position)
        {
            if (piece == null ||
                gridManager == null)
            {
                return false;
            }

            if (!gridManager.IsInsideGrid(
                    position))
            {
                return false;
            }

            switch (piece.Color)
            {
                /*
                 * White는 보드 아래쪽에서 시작.
                 *
                 * 기본값 1:
                 *
                 * y = 0
                 * y = 1
                 *
                 * 두 줄 사용.
                 */
                case PieceColor.White:
                    {
                        return position.y <=
                               whitePlacementMaxY;
                    }

                /*
                 * Black은 보드 위쪽에서 시작.
                 *
                 * GridHeight = 6
                 * blackPlacementDepth = 2라면
                 *
                 * y = 4
                 * y = 5
                 *
                 * 두 줄 사용.
                 */
                case PieceColor.Black:
                    {
                        int minimumBlackY =
                            gridManager.GridHeight -
                            blackPlacementDepth;

                        return position.y >=
                               minimumBlackY;
                    }

                default:
                    return false;
            }
        }

        /*
         * 실제 마우스 클릭 위치를
         * 그리드 좌표로 변환하고 배치를 시도한다.
         */
        private void TrySelectPlacementTile()
        {
            if (targetCamera == null ||
                gridManager == null ||
                placementController == null ||
                placementHighlightManager == null)
            {
                return;
            }

            Ray ray =
                targetCamera.ScreenPointToRay(
                    Input.mousePosition
                );

            bool hitBoard =
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    rayDistance,
                    boardLayerMask,
                    QueryTriggerInteraction.Collide
                );

            /*
             * 보드 자체를 클릭하지 않았다면
             * 현재 배치 모드를 취소한다.
             */
            if (!hitBoard)
            {
                Debug.Log(
                    "[PlacementTargetSelector] " +
                    "보드 밖 클릭 → 배치 취소"
                );

                CancelPlacement();
                return;
            }

            /*
             * World 좌표
             * → Grid 좌표
             */
            bool converted =
                gridManager.TryWorldToGrid(
                    hit.point,
                    out Vector2Int gridPosition
                );

            if (!converted)
            {
                Debug.Log(
                    "[PlacementTargetSelector] " +
                    "그리드 변환 실패 → 배치 취소"
                );

                CancelPlacement();
                return;
            }

            /*
             * 하이라이트 영역 자체가 아닌 곳을 클릭.
             *
             * 예:
             * White인데 보드 중앙이나 Black 진영 클릭.
             */
            if (!placementHighlightManager
                    .IsHighlightedPosition(
                        gridPosition))
            {
                Debug.Log(
                    $"[PlacementTargetSelector] " +
                    $"배치 영역 밖 클릭 | " +
                    $"Position={gridPosition}"
                );

                CancelPlacement();
                return;
            }

            /*
             * 빨간색 하이라이트.
             *
             * 영역 자체는 맞지만
             * 다른 말/장애물 때문에 배치 불가능.
             *
             * 이 경우 배치 모드는 취소하지 않는다.
             */
            if (placementHighlightManager
                    .IsInvalidPosition(
                        gridPosition))
            {
                Debug.Log(
                    $"[PlacementTargetSelector] " +
                    $"배치 불가능 위치 | " +
                    $"Position={gridPosition}"
                );

                return;
            }

            /*
             * Valid가 아니면 안전하게 종료.
             */
            if (!placementHighlightManager
                    .IsValidPosition(
                        gridPosition))
            {
                return;
            }

            ChessPiece piece =
                placementController
                    .CurrentPlacementPiece;

            /*
             * 실제 배치 시도.
             */
            bool placed =
                placementController
                    .TryPlaceCurrentPiece(
                        gridPosition
                    );

            if (!placed)
            {
                Debug.LogWarning(
                    $"[PlacementTargetSelector] " +
                    $"배치 실패 | " +
                    $"Piece=" +
                    $"{GetPieceName(piece)} | " +
                    $"Position={gridPosition}"
                );

                /*
                 * 배치 도중 보드 상태가 바뀌었을 수도
                 * 있으므로 하이라이트를 다시 계산한다.
                 */
                RefreshHighlights();

                return;
            }

            Debug.Log(
                $"[PlacementTargetSelector] " +
                $"배치 성공 | " +
                $"Piece=" +
                $"{GetPieceName(piece)} | " +
                $"Position={gridPosition}"
            );

            ClearHighlights();
        }

        /*
         * 현재 보드 상태를 기준으로
         * 배치 가능/불가능 영역을 다시 계산한다.
         */
        public void RefreshHighlights()
        {
            if (placementController == null ||
                !placementController
                    .IsPlacementMode)
            {
                ClearHighlights();
                return;
            }

            ChessPiece piece =
                placementController
                    .CurrentPlacementPiece;

            if (piece == null)
            {
                ClearHighlights();
                return;
            }

            ShowPlacementHighlights(
                piece
            );
        }

        /*
         * 현재 Placement 선택 취소.
         */
        private void CancelPlacement()
        {
            placementController?
                .CancelCurrentPlacement();

            ClearHighlights();

            Debug.Log(
                "[PlacementTargetSelector] " +
                "배치 선택 취소"
            );
        }

        /*
         * 모든 배치 하이라이트 상태 제거.
         */
        private void ClearHighlights()
        {
            placementHighlightManager?
                .ClearHighlights();

            validPositions.Clear();
            invalidPositions.Clear();

            highlightedPiece = null;
            isShowingHighlights = false;
        }

        private static string GetPieceName(
            ChessPiece piece)
        {
            return piece != null
                ? piece.name
                : "None";
        }

        private void OnDisable()
        {
            ClearHighlights();
        }
    }
}