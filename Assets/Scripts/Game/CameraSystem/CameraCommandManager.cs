using Game.UI.PieceStatus;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraSystem
{
    public sealed class CameraCommandManager : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private CameraController cameraController;

        [SerializeField]
        private PieceWorldUIManager pieceWorldUIManager;

        [Header("Execution")]
        [Tooltip(
            "꺼져 있으면 새 명령이 기존 전환을 즉시 교체합니다. " +
            "켜져 있으면 등록된 순서대로 실행합니다."
        )]
        [SerializeField]
        private bool useCommandQueue;

        private readonly Queue<CameraCommand>
            commandQueue = new();

        private bool isExecuting;

        public bool IsExecuting => isExecuting;

        private void Awake()
        {
            if (cameraController == null)
            {
                cameraController =
                    FindFirstObjectByType<CameraController>();
            }

            if (pieceWorldUIManager == null)
            {
                pieceWorldUIManager =
                    FindFirstObjectByType<PieceWorldUIManager>();
            }
        }

        public void Execute(CameraCommand command)
        {
            if (cameraController == null)
            {
                Debug.LogWarning(
                    "[CameraCommandManager] " +
                    "CameraController가 연결되지 않았습니다.",
                    this
                );

                command.OnComplete?.Invoke();
                return;
            }

            if (!useCommandQueue)
            {
                commandQueue.Clear();
                ExecuteImmediately(command);
                return;
            }

            commandQueue.Enqueue(command);
            TryExecuteNext();
        }

        public void ClearCommands()
        {
            commandQueue.Clear();
        }

        public void ReturnToGrid(
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.ReturnToGrid,
                    null,
                    () =>
                    {
                        if (pieceWorldUIManager != null)
                        {
                            pieceWorldUIManager
                                .RestoreHiddenPieceUI();
                        }

                        onComplete?.Invoke();
                    }
                )
            );
        }

        public void ShowPiece(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceClick,
                    piece,
                    onComplete
                )
            );
        }

        public void ShowMoveRange(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceMove,
                    piece,
                    onComplete
                )
            );
        }

        public void FollowMovingPiece(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceMoving,
                    piece,
                    onComplete
                )
            );
        }

        public void ShowDirectAttack(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceDirectAttack,
                    piece,
                    onComplete
                )
            );
        }

        public void ShowSkillRange(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceSkill,
                    piece,
                    onComplete
                )
            );
        }

        public void ShowSkillEngaging(
            Transform piece,
            System.Action onComplete = null)
        {
            Execute(
                new CameraCommand(
                    CameraCommandType.PieceSkillEngaging,
                    piece,
                    onComplete
                )
            );
        }

        private void TryExecuteNext()
        {
            if (isExecuting ||
                commandQueue.Count == 0)
            {
                return;
            }

            ExecuteImmediately(
                commandQueue.Dequeue()
            );
        }

        private void ExecuteImmediately(
            CameraCommand command)
        {
            isExecuting = true;

            cameraController.ExecuteCommand(
                command,
                () =>
                {
                    isExecuting = false;

                    command.OnComplete?.Invoke();

                    if (useCommandQueue)
                    {
                        TryExecuteNext();
                    }
                }
            );
        }
    }
}