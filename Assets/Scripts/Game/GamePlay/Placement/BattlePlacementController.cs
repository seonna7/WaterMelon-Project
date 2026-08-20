using Game.Core;
using Game.GamePlay.Grid;
using UnityEngine;

namespace Game.GamePlay.Placement
{
    public sealed class BattlePlacementController
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private BattlePieceSpawner
            pieceSpawner;

        private ChessPiece
            currentPlacementPiece;

        private bool isPlacementMode;

        public bool IsPlacementMode =>
            isPlacementMode;

        public ChessPiece
            CurrentPlacementPiece =>
                currentPlacementPiece;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<
                        GameManager>();
            }

            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (pieceSpawner == null)
            {
                pieceSpawner =
                    FindFirstObjectByType<
                        BattlePieceSpawner>();
            }
        }

        /*
         * Pick 단계에서 선택한 프리팹을
         * Placement용 말로 생성한다.
         */
        public bool BeginPlacement(
            ChessPiece prefab,
            PieceColor color)
        {
            if (isPlacementMode)
            {
                Debug.LogWarning(
                    "[Placement] " +
                    "이미 다른 말을 배치 중입니다."
                );

                return false;
            }

            if (pieceSpawner == null ||
                prefab == null)
            {
                return false;
            }

            ChessPiece piece =
                pieceSpawner.CreatePiece(
                    prefab,
                    color
                );

            if (piece == null)
                return false;

            currentPlacementPiece =
                piece;

            isPlacementMode = true;

            Debug.Log(
                $"[Placement] " +
                $"배치 시작 | " +
                $"Piece={piece.name} | " +
                $"Team={color}"
            );

            return true;
        }

        /*
         * 플레이어가 선택한 그리드 위치에
         * 현재 말을 배치한다.
         */
        public bool TryPlaceCurrentPiece(
            Vector2Int gridPosition)
        {
            if (!isPlacementMode ||
                currentPlacementPiece == null)
            {
                return false;
            }

            bool placed =
                pieceSpawner.PlacePiece(
                    currentPlacementPiece,
                    gridPosition
                );

            if (!placed)
                return false;

            Debug.Log(
                $"[Placement] " +
                $"배치 완료 | " +
                $"Piece=" +
                $"{currentPlacementPiece.name} | " +
                $"Position={gridPosition}"
            );

            currentPlacementPiece = null;
            isPlacementMode = false;

            CheckPlacementCompleted();

            return true;
        }

        public void CancelCurrentPlacement()
        {
            if (!isPlacementMode)
                return;

            /*
             * 아직 배치 전인 말을 제거할지는
             * Pick 시스템과 연결할 때 결정한다.
             *
             * 현재는 선택 상태만 종료한다.
             */
            currentPlacementPiece = null;
            isPlacementMode = false;

            Debug.Log(
                "[Placement] 배치 선택 취소"
            );
        }

        private void CheckPlacementCompleted()
        {
            if (pieceSpawner == null ||
                gameManager == null)
            {
                return;
            }

            if (!pieceSpawner.AreAllPiecesPlaced())
            {

                return;
            }

            Debug.Log(
                "[Placement] 모든 말 배치 완료 → Battle 시작"
            );

            gameManager.StartBattlePhase();
        }
    }
}