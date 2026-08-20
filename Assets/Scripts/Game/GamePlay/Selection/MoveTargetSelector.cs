using Game.CameraSystem;
using Game.Core;
using Game.GamePlay.Grid;
using Game.GamePlay.Preview;
using Game.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Selection
{
    public sealed class MoveTargetSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CameraCommandManager cameraCommandManager;
        [SerializeField] private MoveHighlightManager highlightManager;
        [SerializeField] private PieceActionPreviewController previewController;

        [Header("Raycast")]
        [SerializeField] private LayerMask boardLayerMask;
        [SerializeField] private float rayDistance = 100f;

        private readonly HashSet<Vector2Int> movePositions =
            new HashSet<Vector2Int>();

        private readonly HashSet<Vector2Int> attackPositions =
            new HashSet<Vector2Int>();

        private ChessPiece selectedPiece;
        private bool isActive;
        private bool isExecuting;
        private bool hasPreviewPosition;
        private Vector2Int previewPosition;

        public bool IsMoveMode => isActive;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (gridManager == null)
                gridManager = FindFirstObjectByType<GridManager>();

            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (cameraCommandManager == null)
            {
                cameraCommandManager =
                    FindFirstObjectByType<CameraCommandManager>();
            }

            if (highlightManager == null)
            {
                highlightManager =
                    FindFirstObjectByType<MoveHighlightManager>();
            }

            if (previewController == null)
            {
                previewController =
                    FindFirstObjectByType<
                        PieceActionPreviewController>();
            }
        }

        private void Update()
        {
            if (!isActive || isExecuting)
                return;

            if (Input.GetMouseButtonDown(1))
            {
                CancelMoveMode();
                return;
            }

            UpdatePreview();

            if (Input.GetMouseButtonDown(0))
                SelectCurrentCell();
        }

        public void EnterMoveMode(
            ChessPiece piece)
        {
            ExitMoveMode(returnToGrid: false);

            if (piece == null ||
                piece.IsDead ||
                !piece.IsPlaced ||
                gridManager == null ||
                highlightManager == null)
            {
                return;
            }

            selectedPiece = piece;
            isActive = true;
            isExecuting = false;

            RefreshPositions();
        }

        private void RefreshPositions()
        {
            movePositions.Clear();
            attackPositions.Clear();
            ClearPreview();

            if (selectedPiece == null)
                return;

            List<Vector2Int> moves =
                selectedPiece.GetPossibleMoves(gridManager);

            for (int i = 0; i < moves.Count; i++)
            {
                Vector2Int position = moves[i];

                if (gridManager.IsEmpty(position))
                    movePositions.Add(position);
            }

            List<Vector2Int> attacks =
                selectedPiece.GetDirectAttackPositions(gridManager);

            for (int i = 0; i < attacks.Count; i++)
            {
                Vector2Int position = attacks[i];
                ChessPiece target = gridManager.GetPieceAt(position);

                if (target == null ||
                    target.IsDead ||
                    target.Color == selectedPiece.Color)
                {
                    continue;
                }

                attackPositions.Add(position);
                movePositions.Remove(position);
            }

            highlightManager.ShowHighlights(
                movePositions,
                attackPositions
            );
        }

        private void UpdatePreview()
        {
            if (previewController == null ||
                !TryGetGridUnderMouse(out Vector2Int position))
            {
                ClearPreview();
                return;
            }

            bool isMove = movePositions.Contains(position);
            bool isAttack = attackPositions.Contains(position);

            if (!isMove && !isAttack)
            {
                ClearPreview();
                return;
            }

            if (hasPreviewPosition &&
                previewPosition == position)
            {
                return;
            }

            hasPreviewPosition = true;
            previewPosition = position;

            if (isAttack)
            {
                previewController.ShowDirectAttack(
                    selectedPiece,
                    gridManager.GetPieceAt(position),
                    gridManager
                );
            }
            else
            {
                previewController.ShowMove(
                    selectedPiece,
                    position,
                    gridManager
                );
            }
        }

        private void SelectCurrentCell()
        {
            if (!TryGetGridUnderMouse(out Vector2Int position))
            {
                CancelMoveMode();
                return;
            }

            if (attackPositions.Contains(position))
            {
                ExecuteAttack(position);
                return;
            }

            if (movePositions.Contains(position))
            {
                ExecuteMove(position);
                return;
            }

            CancelMoveMode();
        }

        private void ExecuteAttack(
            Vector2Int position)
        {
            if (selectedPiece == null ||
                gameManager == null ||
                gameManager.PieceActionController == null)
            {
                return;
            }

            ChessPiece target = gridManager.GetPieceAt(position);

            if (target == null ||
                target.IsDead ||
                target.Color == selectedPiece.Color)
            {
                RefreshPositions();
                return;
            }

            isExecuting = true;
            ClearVisuals();

            Game.Action.ActionResult result =
                gameManager.PieceActionController.TryAttackPiece(
                    selectedPiece,
                    target
                );

            if (result.Success)
            {
                ExitMoveMode();
                return;
            }

            isExecuting = false;
            RefreshPositions();
        }

        private void ExecuteMove(
            Vector2Int destination)
        {
            if (selectedPiece == null ||
                !gridManager.IsEmpty(destination))
            {
                return;
            }

            ChessPiece piece = selectedPiece;

            isExecuting = true;
            ClearVisuals();

            if (cameraCommandManager != null)
            {
                cameraCommandManager.FollowMovingPiece(
                    piece.transform,
                    () => MovePiece(piece, destination)
                );
            }
            else
            {
                MovePiece(piece, destination);
            }
        }

        private void MovePiece(
            ChessPiece piece,
            Vector2Int destination)
        {
            bool success =
                piece != null &&
                gridManager.MovePiece(piece, destination);

            if (success)
            {
                ExitMoveMode();
                return;
            }

            isExecuting = false;
            RefreshPositions();
        }

        private bool TryGetGridUnderMouse(
            out Vector2Int position)
        {
            position = default;

            if (targetCamera == null || gridManager == null)
                return false;

            Ray ray = targetCamera.ScreenPointToRay(
                Input.mousePosition
            );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    rayDistance,
                    boardLayerMask,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            return gridManager.TryWorldToGrid(
                hit.point,
                out position
            );
        }

        public void CancelMoveMode()
        {
            if (!isActive || isExecuting)
                return;

            ExitMoveMode();
        }

        private void ClearPreview()
        {
            hasPreviewPosition = false;
            previewController?.Clear();
        }

        private void ClearVisuals()
        {
            highlightManager?.ClearHighlights();
            ClearPreview();
        }

        private void ExitMoveMode(
            bool returnToGrid = true)
        {
            ClearVisuals();
            movePositions.Clear();
            attackPositions.Clear();

            selectedPiece = null;
            isActive = false;
            isExecuting = false;

            if (returnToGrid)
                cameraCommandManager?.ReturnToGrid();
        }

        private void OnDisable()
        {
            ClearVisuals();
        }
    }
}